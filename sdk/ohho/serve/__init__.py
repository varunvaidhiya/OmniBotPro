"""Policy serving — expose a trained model behind a REST endpoint.

Wraps ``packages/vla_serve`` (FastAPI + pluggable model backend) behind a simple
``serve()`` function and a ``build_app()`` that's testable without launching a
server. When the ``[serve]`` extra (fastapi + uvicorn) is installed, ``serve()``
launches a full inference server; without it, ``build_app()`` still returns a
FastAPI app for testing.

The server is model-agnostic: set ``OHHO_MODEL_CLASS`` to any ``VLAModel``
subclass and it loads on startup (``OHHO_AUTO_LOAD=1``) or on first request.
"""

from __future__ import annotations

from typing import Any, Optional

from ..hardware import resolve_device


class ServeUnavailable(RuntimeError):
    """Raised when the serving backend (fastapi/uvicorn) isn't installed."""


# Pydantic models defined at module level so FastAPI's response_model can resolve them.
try:
    from pydantic import BaseModel

    class InferenceRequest(BaseModel):
        instruction: str
        image_base64: str = ""
        config: dict = {}

    class InferenceResponse(BaseModel):
        action: dict
        raw_output: str = ""
        latency_ms: float = 0.0

    _PYDANTIC_AVAILABLE = True
except ImportError:
    _PYDANTIC_AVAILABLE = False


def build_app(
    model_class: str = "",
    model_path: str = "",
    device: str = "auto",
    api_key: str = "",
    auto_load: bool = False,
    mock_model: bool = False,
):
    """Build a FastAPI inference app (testable without launching).

    Parameters:
        model_class: dotted path to a ``VLAModel`` subclass
            (default: ``vla_serve.models.openvla.OpenVLAModel``).
        model_path: HF hub id or local checkpoint path.
        device: ``"auto"`` or explicit.
        api_key: if non-empty, requires ``X-API-Key`` header.
        auto_load: load the model on startup vs. lazy via ``/load_model``.
        mock_model: if True, inject a no-op model that returns zero actions
            (for the record→train→serve sim loop, no GPU needed).

    Returns:
        A FastAPI ``FastAPI`` application instance.
    """
    try:
        from fastapi import FastAPI, Header, HTTPException
    except ImportError as e:
        raise ServeUnavailable(
            f"Building the serve app needs fastapi: pip install 'ohho-os[serve]'. ({e})"
        ) from e

    if not _PYDANTIC_AVAILABLE:
        raise ServeUnavailable(
            "Building the serve app needs pydantic: pip install 'ohho-os[serve]'."
        )

    app = FastAPI(title="OhhO Serve", version="0.1.0")
    dev = resolve_device(device)
    _model: Any = None
    _model_path = model_path

    def _check_auth(x_api_key: Optional[str] = Header(None)):
        if api_key and x_api_key != api_key:
            raise HTTPException(status_code=401, detail="invalid API key")

    if mock_model:
        _model = _MockModel(dev)
        if model_path:
            _model.load_model(model_path)

    @app.get("/health")
    async def health():
        return {"status": "ok", "model_loaded": _model is not None, "device": dev}

    @app.post("/load_model")
    async def load_model(
        model_path: str = "",
        x_api_key: Optional[str] = Header(None),
    ):
        _check_auth(x_api_key)
        nonlocal _model, _model_path
        path = model_path or _model_path
        if not path:
            raise HTTPException(400, "model_path required")
        _model_path = path
        if mock_model:
            _model = _MockModel(dev)
            _model.load_model(path)
        else:
            cls = _resolve_model_class(model_class)
            _model = cls()
            _model.load_model(path)
        return {"status": "loaded", "model": type(_model).__name__}

    @app.post("/predict", response_model=InferenceResponse)
    async def predict(
        req: InferenceRequest,
        x_api_key: Optional[str] = Header(None),
    ):
        _check_auth(x_api_key)
        if _model is None:
            raise HTTPException(503, "model not loaded — POST /load_model first")
        import time

        t0 = time.monotonic()
        image = _decode_image(req.image_base64) if req.image_base64 else None
        result = _model.predict_action(image, req.instruction, **req.config)
        latency = (time.monotonic() - t0) * 1000
        if isinstance(result, dict):
            action = result
        else:
            action = {"vector": list(result)}
        return InferenceResponse(action=action, latency_ms=latency)

    return app


def serve(
    checkpoint: str = "",
    *,
    port: int = 8000,
    host: str = "0.0.0.0",
    device: str = "auto",
    model_class: str = "",
    api_key: str = "",
    mock: bool = False,
) -> None:
    """Launch the inference server (blocking).

    Parameters:
        checkpoint: path to the trained checkpoint (or HF hub id).
        port, host: bind address.
        device: ``"auto"`` or explicit.
        model_class: dotted path to a ``VLAModel`` subclass.
        api_key: if non-empty, requires ``X-API-Key`` header on mutations.
        mock: if True, use a no-op model (for the sim loop, no GPU needed).
    """
    try:
        import uvicorn
    except ImportError as e:
        raise ServeUnavailable(
            f"Launching the server needs uvicorn: pip install 'ohho-os[serve]'. ({e})"
        ) from e

    app = build_app(
        model_class=model_class,
        model_path=checkpoint,
        device=device,
        api_key=api_key,
        auto_load=bool(checkpoint),
        mock_model=mock,
    )
    uvicorn.run(app, host=host, port=port)


def _resolve_model_class(dotted: str):
    """Import a dotted class path like 'vla_serve.models.openvla.OpenVLAModel'."""
    if not dotted:
        dotted = "vla_serve.models.openvla.OpenVLAModel"
    parts = dotted.rsplit(".", 1)
    if len(parts) != 2:
        raise ValueError(f"invalid model class path: {dotted}")
    module_path, cls_name = parts
    import importlib

    mod = importlib.import_module(module_path)
    return getattr(mod, cls_name)


def _decode_image(b64: str):
    """Decode a base64 image to a PIL Image (best-effort)."""
    try:
        import base64
        import io

        from PIL import Image  # type: ignore
    except ImportError:
        return None
    if "://" in b64:
        b64 = b64.split(",", 1)[-1]
    raw = base64.b64decode(b64)
    return Image.open(io.BytesIO(raw))


class _MockModel:
    """No-op model for the sim record→train→serve loop (no GPU needed)."""

    def __init__(self, device: str) -> None:
        self.device = device
        self.model = None
        self._path = ""

    def load_model(self, model_path: str, **kwargs) -> None:
        self._path = model_path
        self.model = {"path": model_path, "device": self.device}

    def predict_action(self, image, instruction: str, **kwargs):
        return {"vector": [0.0] * 9, "instruction": instruction, "mock": True}


__all__ = ["serve", "build_app", "ServeUnavailable"]
