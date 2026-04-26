"""Request/response schemas for the vla_serve REST API."""

from typing import Any, Dict, Optional
from pydantic import BaseModel


class InferenceRequest(BaseModel):
    instruction: str
    image_base64: (
        str  # Base64-encoded image (JPEG/PNG, with or without data URI prefix)
    )
    config: Optional[Dict[str, Any]] = None


class InferenceResponse(BaseModel):
    action: Dict[
        str, Any
    ]  # Model output — always a dict; action vector under 'vector' key
    raw_output: str  # Raw model text output (for debugging)
    latency_ms: float
