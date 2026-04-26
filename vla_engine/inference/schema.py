from pydantic import BaseModel
from typing import Optional, Dict, Any


class InferenceRequest(BaseModel):
    instruction: str
    image_base64: str  # Base64 encoded image
    config: Optional[Dict[str, Any]] = None


class InferenceResponse(BaseModel):
    action: Dict[str, Any]
    raw_output: str
    latency_ms: float
