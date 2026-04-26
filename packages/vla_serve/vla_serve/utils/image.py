"""Image encode/decode utilities for vla_serve."""

import base64
import io

import numpy as np
from PIL import Image


def decode_base64_image(b64: str) -> Image.Image:
    """Decode a base64 string (with or without data URI prefix) to PIL Image."""
    if "," in b64:
        b64 = b64.split(",", 1)[1]
    data = base64.b64decode(b64)
    return Image.open(io.BytesIO(data)).convert("RGB")


def numpy_to_base64(arr: np.ndarray, fmt: str = "JPEG") -> str:
    """Encode a numpy array (H×W×3 uint8) to a base64 JPEG/PNG string."""
    img = Image.fromarray(arr.astype(np.uint8))
    buf = io.BytesIO()
    img.save(buf, format=fmt)
    return base64.b64encode(buf.getvalue()).decode("utf-8")
