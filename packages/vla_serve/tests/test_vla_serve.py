"""Tests for the vla_serve package.

Covers utils/image, inference/schema, models/base, models/openvla,
and the FastAPI server endpoints.
"""

import base64
import io
from unittest.mock import patch

import numpy as np
import pytest
from fastapi.testclient import TestClient
from PIL import Image

from vla_serve.inference.schema import InferenceRequest, InferenceResponse
from vla_serve.inference.server import app
from vla_serve.models.base import VLAModel
from vla_serve.utils.image import decode_base64_image, numpy_to_base64


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _jpeg_b64(w: int = 8, h: int = 8) -> str:
    img = Image.fromarray(np.zeros((h, w, 3), dtype=np.uint8))
    buf = io.BytesIO()
    img.save(buf, format="JPEG")
    return base64.b64encode(buf.getvalue()).decode()


class _FakeVLAModel(VLAModel):
    """Lightweight stand-in — no ML libraries required."""

    model = None  # None → not loaded

    def load_model(self, model_path: str, **kwargs) -> None:
        self.model = object()

    def predict_action(self, image, instruction, **kwargs):
        return {"vector": [0.1, 0.2, 0.3]}


@pytest.fixture
def http_client():
    with patch(
        "vla_serve.inference.server._get_model_class",
        return_value=_FakeVLAModel,
    ):
        with TestClient(app) as c:
            yield c


# ---------------------------------------------------------------------------
# vla_serve.utils.image
# ---------------------------------------------------------------------------


def test_decode_plain_b64():
    img = decode_base64_image(_jpeg_b64())
    assert isinstance(img, Image.Image)
    assert img.mode == "RGB"


def test_decode_data_uri_prefix():
    b64 = "data:image/jpeg;base64," + _jpeg_b64()
    img = decode_base64_image(b64)
    assert isinstance(img, Image.Image)


def test_numpy_to_base64_jpeg():
    arr = np.zeros((8, 8, 3), dtype=np.uint8)
    result = numpy_to_base64(arr)
    assert isinstance(result, str)
    Image.open(io.BytesIO(base64.b64decode(result)))  # must not raise


def test_numpy_to_base64_png():
    arr = np.zeros((8, 8, 3), dtype=np.uint8)
    result = numpy_to_base64(arr, fmt="PNG")
    assert isinstance(result, str)
    img = Image.open(io.BytesIO(base64.b64decode(result)))
    assert img.format == "PNG"


# ---------------------------------------------------------------------------
# vla_serve.inference.schema
# ---------------------------------------------------------------------------


def test_inference_request_minimal():
    req = InferenceRequest(instruction="pick up cup", image_base64="abc")
    assert req.instruction == "pick up cup"
    assert req.config is None


def test_inference_request_with_config():
    req = InferenceRequest(
        instruction="test", image_base64="x", config={"max_tokens": 64}
    )
    assert req.config == {"max_tokens": 64}


def test_inference_response_fields():
    resp = InferenceResponse(
        action={"vector": [0.1, 0.2]},
        raw_output="raw text",
        latency_ms=3.5,
    )
    assert resp.action == {"vector": [0.1, 0.2]}
    assert resp.raw_output == "raw text"
    assert resp.latency_ms == pytest.approx(3.5)


# ---------------------------------------------------------------------------
# vla_serve.models.base
# ---------------------------------------------------------------------------


def test_vla_model_abstract():
    with pytest.raises(TypeError):
        VLAModel()


def test_vla_model_concrete_subclass():
    m = _FakeVLAModel()
    m.load_model("some/path")
    assert m.model is not None
    result = m.predict_action(None, "test")
    assert isinstance(result, dict)


# ---------------------------------------------------------------------------
# vla_serve.models.openvla
# ---------------------------------------------------------------------------


def test_openvla_init():
    from vla_serve.models.openvla import OpenVLAModel

    m = OpenVLAModel()
    assert m.model is None
    assert m.processor is None
    assert m._device in ("cuda", "cpu")


def test_openvla_detect_device_no_torch():
    from vla_serve.models.openvla import OpenVLAModel

    with patch.dict("sys.modules", {"torch": None}):
        device = OpenVLAModel._detect_device()
    assert device == "cpu"


def test_openvla_predict_unloaded_raises():
    from vla_serve.models.openvla import OpenVLAModel

    m = OpenVLAModel()
    with pytest.raises(RuntimeError, match="not loaded"):
        m.predict_action(None, "do something")


# ---------------------------------------------------------------------------
# vla_serve.inference.server — endpoints
# ---------------------------------------------------------------------------


def test_health_model_not_loaded(http_client):
    r = http_client.get("/health")
    assert r.status_code == 200
    body = r.json()
    assert body["status"] == "ok"
    assert body["model_loaded"] is False


def test_health_model_loaded(http_client):
    from vla_serve.inference import server as srv

    original = srv._model.model
    try:
        srv._model.model = object()
        r = http_client.get("/health")
        assert r.json()["model_loaded"] is True
    finally:
        srv._model.model = original


def test_load_model_success(http_client):
    r = http_client.post("/load_model", params={"model_path": "test/model"})
    assert r.status_code == 200
    data = r.json()
    assert data["status"] == "success"
    assert data["model"] == "test/model"


def test_load_model_default_path(http_client):
    r = http_client.post("/load_model")
    assert r.status_code == 200
    assert r.json()["status"] == "success"


def test_load_model_with_4bit(http_client):
    r = http_client.post(
        "/load_model", params={"model_path": "x/model", "load_4bit": True}
    )
    assert r.status_code == 200


def test_predict_model_not_loaded_returns_503(http_client):
    from vla_serve.inference import server as srv

    original = srv._model.model
    try:
        srv._model.model = None
        r = http_client.post(
            "/predict",
            json={"instruction": "test", "image_base64": _jpeg_b64()},
        )
        assert r.status_code == 503
    finally:
        srv._model.model = original


def test_predict_success(http_client):
    from vla_serve.inference import server as srv

    original = srv._model
    try:
        loaded = _FakeVLAModel()
        loaded.model = object()
        srv._model = loaded
        r = http_client.post(
            "/predict",
            json={"instruction": "pick up cup", "image_base64": _jpeg_b64()},
        )
        assert r.status_code == 200
        data = r.json()
        assert "action" in data
        assert "latency_ms" in data
    finally:
        srv._model = original


# ---------------------------------------------------------------------------
# vla_serve.inference.server — authentication
# ---------------------------------------------------------------------------


def test_no_api_key_open_access(http_client):
    from vla_serve.inference import server as srv

    orig = srv._API_KEY
    try:
        srv._API_KEY = ""
        r = http_client.post("/load_model", params={"model_path": "x"})
        assert r.status_code == 200
    finally:
        srv._API_KEY = orig


def test_wrong_api_key_rejected(http_client):
    from vla_serve.inference import server as srv

    orig = srv._API_KEY
    try:
        srv._API_KEY = "secret"
        r = http_client.post(
            "/load_model",
            params={"model_path": "x"},
            headers={"X-API-Key": "wrong"},
        )
        assert r.status_code == 401
    finally:
        srv._API_KEY = orig


def test_correct_api_key_accepted(http_client):
    from vla_serve.inference import server as srv

    orig = srv._API_KEY
    try:
        srv._API_KEY = "secret"
        r = http_client.post(
            "/load_model",
            params={"model_path": "x"},
            headers={"X-API-Key": "secret"},
        )
        assert r.status_code == 200
    finally:
        srv._API_KEY = orig


def test_rate_limit_not_applied_without_auth(http_client):
    from vla_serve.inference import server as srv

    orig = srv._API_KEY
    try:
        srv._API_KEY = ""
        for _ in range(15):
            r = http_client.get("/health")
            assert r.status_code == 200
    finally:
        srv._API_KEY = orig
