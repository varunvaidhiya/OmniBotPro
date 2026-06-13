"""
vla_serve FastAPI inference server.

Environment variables
---------------------
VLA_MODEL_CLASS   Module path to model class (default: vla_serve.models.openvla.OpenVLAModel)
VLA_MODEL_PATH    HuggingFace model ID or local path (default: openvla/openvla-7b)
VLA_LOAD_4BIT     '1' to enable 4-bit quantization (default: '0')
VLA_AUTO_LOAD     '1' to load model on startup (default: '0' — use /load_model)
VLA_API_KEY       API key required on /predict and /load_model (disabled if empty)
VLA_RATE_LIMIT    Max requests/second per key on /predict (default: 10)
LOG_LEVEL         Python logging level (default: INFO)
"""

import importlib
import logging
import os
import time
from collections import defaultdict
from contextlib import asynccontextmanager
from typing import Optional

import uvicorn
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security.api_key import APIKeyHeader

from .schema import InferenceRequest, InferenceResponse
from ..utils.image import decode_base64_image
from ..models.base import VLAModel

_MAX_BODY_BYTES = 10 * 1024 * 1024

try:
    from prometheus_fastapi_instrumentator import Instrumentator
    from prometheus_client import Histogram

    _PROM_AVAILABLE = True
    VLA_INFERENCE_HIST = Histogram(
        "vla_inference_seconds",
        "VLA model inference duration in seconds",
        buckets=[0.05, 0.1, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0],
    )
    VLA_PREPROCESS_HIST = Histogram(
        "vla_preprocess_seconds",
        "VLA image preprocess duration in seconds",
        buckets=[0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0],
    )
except ImportError:
    _PROM_AVAILABLE = False

logger = logging.getLogger("vla_serve")

logging.basicConfig(
    level=os.getenv("LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
)

# ---------------------------------------------------------------------------
# Globals
# ---------------------------------------------------------------------------

_model: Optional[VLAModel] = None

# Simple in-process token-bucket rate limiter: {api_key: (tokens, last_refill)}
_rate_buckets: dict[str, tuple[float, float]] = defaultdict(
    lambda: (float(os.getenv("VLA_RATE_LIMIT", "10")), time.monotonic())
)
_RATE_LIMIT = float(os.getenv("VLA_RATE_LIMIT", "10"))  # requests per second


def _get_model_class() -> type:
    cls_path = os.getenv("VLA_MODEL_CLASS", "vla_serve.models.openvla.OpenVLAModel")
    module_path, cls_name = cls_path.rsplit(".", 1)
    module = importlib.import_module(module_path)
    return getattr(module, cls_name)


# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------

_API_KEY: str = os.getenv("VLA_API_KEY", "")
_api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)


def _require_api_key(api_key: Optional[str] = Depends(_api_key_header)) -> str:
    """Dependency: validate X-API-Key header if VLA_API_KEY is configured."""
    if not _API_KEY:
        # Auth disabled — open access (development / local use)
        return ""
    if api_key != _API_KEY:
        logger.warning("rejected request with invalid API key")
        raise HTTPException(status_code=401, detail="Invalid or missing API key.")
    return api_key


def _check_rate_limit(key: str) -> None:
    """Token-bucket rate limiter — raises HTTP 429 when exhausted."""
    now = time.monotonic()
    tokens, last = _rate_buckets[key]
    elapsed = now - last
    tokens = min(_RATE_LIMIT, tokens + elapsed * _RATE_LIMIT)

    if tokens < 1.0:
        raise HTTPException(
            status_code=429,
            detail=f"Rate limit exceeded. Max {_RATE_LIMIT:.0f} requests/second.",
        )

    _rate_buckets[key] = (tokens - 1.0, now)


# ---------------------------------------------------------------------------
# Lifespan
# ---------------------------------------------------------------------------


@asynccontextmanager
async def lifespan(app: FastAPI):
    global _model
    cls = _get_model_class()
    _model = cls()
    logger.info("model class = %s", cls.__name__)
    auth_status = "enabled" if _API_KEY else "DISABLED (set VLA_API_KEY to enable)"
    logger.info("API key authentication: %s", auth_status)

    if os.getenv("VLA_AUTO_LOAD", "0") == "1":
        model_path = os.getenv("VLA_MODEL_PATH", "openvla/openvla-7b")
        load_4bit = os.getenv("VLA_LOAD_4BIT", "0") == "1"
        logger.info("auto-loading %s (4bit=%s)", model_path, load_4bit)
        _model.load_model(model_path=model_path, load_in_4bit=load_4bit)
        logger.info("model loaded successfully")

    yield

    if _model is not None:
        try:
            import torch
            torch.cuda.empty_cache()
        except Exception:
            pass
        del _model


# ---------------------------------------------------------------------------
# App
# ---------------------------------------------------------------------------

app = FastAPI(
    title="vla_serve",
    description="Model-agnostic VLA inference REST server",
    version="1.0.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("VLA_CORS_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["GET", "POST"],
    allow_headers=["X-API-Key", "Content-Type"],
)

_MB = 1024 * 1024
_MAX_BODY = int(os.getenv("VLA_MAX_BODY_BYTES", str(_MAX_BODY_BYTES)))


@app.middleware("http")
async def _limit_body_size(request: Request, call_next):
    content_length = request.headers.get("content-length")
    if content_length and int(content_length) > _MAX_BODY:
        raise HTTPException(status_code=413, detail="Request body too large")
    return await call_next(request)

# Instrument all HTTP routes with Prometheus metrics (/metrics endpoint).
# Exposes: http_requests_total, http_request_duration_seconds, etc.
if _PROM_AVAILABLE:
    Instrumentator(
        should_group_status_codes=True,
        should_ignore_untemplated=True,
        excluded_handlers=["/metrics", "/health"],
    ).instrument(app).expose(app, include_in_schema=False)


@app.get("/health")
def health():
    """Health check — no auth required."""
    loaded = _model is not None and getattr(_model, "model", None) is not None
    return {"status": "ok", "model_loaded": loaded}


@app.post("/load_model")
def load_model(
    model_path: str = "openvla/openvla-7b",
    load_4bit: bool = False,
    _key: str = Depends(_require_api_key),
):
    if _model is None:
        raise HTTPException(status_code=503, detail="Server not initialized.")
    try:
        logger.info("loading model %s (4bit=%s)", model_path, load_4bit)
        _model.load_model(model_path=model_path, load_in_4bit=load_4bit)
        logger.info("model %s loaded", model_path)
        return {"status": "success", "model": model_path}
    except Exception as exc:
        logger.error("load_model failed: %s", exc)
        raise HTTPException(status_code=500, detail=str(exc))


@app.post("/predict", response_model=InferenceResponse)
def predict(
    request: InferenceRequest,
    key: str = Depends(_require_api_key),
):
    _check_rate_limit(key or "anon")

    if _model is None or getattr(_model, "model", None) is None:
        raise HTTPException(
            status_code=503, detail="Model not loaded. POST to /load_model first."
        )
    try:
        t0 = time.time()

        t_pre = time.time()
        image = decode_base64_image(request.image_base64)
        preprocess_s = time.time() - t_pre
        if _PROM_AVAILABLE:
            VLA_PREPROCESS_HIST.observe(preprocess_s)

        t_inf = time.time()
        result = _model.predict_action(
            image, request.instruction, **(request.config or {})
        )
        inference_s = time.time() - t_inf
        if _PROM_AVAILABLE:
            VLA_INFERENCE_HIST.observe(inference_s)

        latency = (time.time() - t0) * 1000.0
        logger.debug(
            "inference latency=%.1f ms instruction=%r", latency, request.instruction
        )
        if isinstance(result, dict) and "vector" in result:
            action = result
        elif isinstance(result, (list, tuple)):
            action = {"vector": list(result), "raw_output": str(result)}
        else:
            action = {"vector": [], "raw_output": str(result)}
        return InferenceResponse(
            action=action,
            raw_output=action.get("raw_output", str(result)),
            latency_ms=latency,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("predict failed: %s", exc)
        raise HTTPException(status_code=500, detail=str(exc))


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main():
    uvicorn.run(
        "vla_serve.inference.server:app",
        host="0.0.0.0",
        port=int(os.getenv("VLA_PORT", "8000")),
        reload=False,
    )


if __name__ == "__main__":
    main()
