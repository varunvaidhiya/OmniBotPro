import base64
import io
from PIL import Image
import numpy as np


def decode_base64_image(base64_string: str) -> Image.Image:
    """
    Decode a base64 string into a PIL Image.
    """
    if "," in base64_string:
        base64_string = base64_string.split(",")[1]

    image_data = base64.b64decode(base64_string)
    image = Image.open(io.BytesIO(image_data))
    return image.convert("RGB")


def numpy_to_base64(arr: np.ndarray) -> str:
    """
    Convert a numpy array to a base64 string.
    """
    img = Image.fromarray(arr)
    buffered = io.BytesIO()
    img.save(buffered, format="JPEG")
    return base64.b64encode(buffered.getvalue()).decode("utf-8")
