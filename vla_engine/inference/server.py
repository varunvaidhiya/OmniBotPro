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

from ..models.openvla import OpenVLAModel
from ..utils.image import decode_base64_image
from .schema import InferenceRequest, InferenceResponse

logger = logging.getLogger("vla_engine")

_MAX_BODY_BYTES = 10 * 1024 * 1024

# ---------------------------------------------------------------------------
# Globals
# ---------------------------------------------------------------------------
model_instance: Optional[OpenVLAModel] = None

_rate_buckets: dict[str, tuple[float, float]] = defaultdict(
    lambda: (float(os.getenv("VLA_RATE_LIMIT", "10")), time.monotonic())
)
_RATE_LIMIT = float(os.getenv("VLA_RATE_LIMIT", "10"))

# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------
_API_KEY: str = os.getenv("VLA_API_KEY", "")
_api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)


def _require_api_key(api_key: Optional[str] = Depends(_api_key_header)) -> str:
    if not _API_KEY:
        return ""
    if api_key != _API_KEY:
        logger.warning("rejected request with invalid API key")
        raise HTTPException(status_code=401, detail="Invalid or missing API key.")
    return api_key


def _check_rate_limit(key: str) -> None:
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
    global model_instance
    logger.info("Initializing VLA Engine Inference Server...")
    try:
        model_instance = OpenVLAModel()
    except Exception as e:
        logger.error("Error initializing model: %s", e)

    yield

    if model_instance is not None:
        try:
            import torch

            torch.cuda.empty_cache()
        except Exception:
            pass
        del model_instance


# ---------------------------------------------------------------------------
# App
# ---------------------------------------------------------------------------
app = FastAPI(lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("VLA_CORS_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["GET", "POST"],
    allow_headers=["X-API-Key", "Content-Type"],
)

_MAX_BODY = int(os.getenv("VLA_MAX_BODY_BYTES", str(_MAX_BODY_BYTES)))


@app.middleware("http")
async def _limit_body_size(request: Request, call_next):
    content_length = request.headers.get("content-length")
    if content_length and int(content_length) > _MAX_BODY:
        raise HTTPException(status_code=413, detail="Request body too large")
    return await call_next(request)


@app.get("/health")
def health_check():
    loaded = (
        model_instance is not None
        and getattr(model_instance, "model", None) is not None
    )
    return {"status": "ok", "model_loaded": loaded}


@app.post("/load_model")
def load_model(
    model_path: str = "openvla/openvla-7b",
    load_4bit: bool = False,
    _key: str = Depends(_require_api_key),
):
    global model_instance
    if not model_instance:
        model_instance = OpenVLAModel()

    try:
        model_instance.load_model(model_path=model_path, load_in_4bit=load_4bit)
        return {"status": "success", "message": f"Loaded {model_path}"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/predict", response_model=InferenceResponse)
def predict(
    request: InferenceRequest,
    key: str = Depends(_require_api_key),
):
    _check_rate_limit(key or "anon")

    global model_instance
    if not model_instance or not model_instance.model:
        raise HTTPException(
            status_code=503, detail="Model not loaded. Call /load_model first."
        )

    try:
        start_time = time.time()
        image = decode_base64_image(request.image_base64)
        result = model_instance.predict_action(image, request.instruction)

        latency = (time.time() - start_time) * 1000
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
    except Exception as e:
        logger.error("predict failed: %s", e)
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
