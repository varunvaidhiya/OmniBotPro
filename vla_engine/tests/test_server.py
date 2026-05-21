import sys
import os
import io
import base64
import numpy as np
import pytest
from PIL import Image
from unittest.mock import patch
from fastapi.testclient import TestClient

# Skip entire module when torch (and other heavy GPU deps) are not installed
pytest.importorskip(
    "torch", reason="torch not installed — vla_engine tests require GPU dependencies"
)

# Add vla_engine to path
sys.path.append(os.path.join(os.path.dirname(__file__), ".."))

# Mock the model before importing server
with patch("vla_engine.models.openvla.OpenVLAModel") as MockModel:
    from vla_engine.inference.server import app

client = TestClient(app)


def create_base64_image():
    arr = np.random.randint(0, 255, (224, 224, 3), dtype=np.uint8)
    img = Image.fromarray(arr)
    buffered = io.BytesIO()
    img.save(buffered, format="JPEG")
    return base64.b64encode(buffered.getvalue()).decode("utf-8")


def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    assert "status" in response.json()


@patch("vla_engine.inference.server.model_instance")
def test_predict_endpoint(mock_instance):
    # Mock the prediction
    mock_instance.model = True  # Simulate loaded
    mock_instance.predict_action.return_value = {"vector": [0.1, 0.2, 0.3]}

    img_b64 = create_base64_image()
    payload = {"instruction": "move forward", "image_base64": img_b64}

    response = client.post("/predict", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert "action" in data
    assert data["action"]["vector"] == [0.1, 0.2, 0.3]
    assert "latency_ms" in data


def test_load_model_endpoint():
    with patch("vla_engine.inference.server.model_instance"):
        response = client.post(
            "/load_model", params={"model_path": "fake/path", "load_4bit": False}
        )
        # Note: Since we are mocking the Global variable in the test scope,
        # checking the side effect on the server module's global might be tricky if not careful.
        # However, for this basic test, we just want to ensure the endpoint is reachable.
        # The real server uses global model_instance.
        assert response.status_code == 200
