"""VLM clients used as AI judges across the evaluation and reward layers.

``ClaudeVLMClient`` is the default foundation-model judge (the project
already depends on Anthropic for omnibot_orchestration). Any provider can
be plugged in by implementing ``VLMClient``.
"""

from __future__ import annotations

import io
import os
from typing import Optional, Sequence

import numpy as np

from ..core.interfaces import VLMClient


class ClaudeVLMClient(VLMClient):
    def __init__(
        self,
        model: str = "claude-fable-5",
        api_key: str = "",
        max_tokens: int = 1024,
    ) -> None:
        try:
            import anthropic
        except ImportError as e:
            raise RuntimeError(
                "ClaudeVLMClient requires `pip install anthropic`"
            ) from e
        self.model = model
        self.max_tokens = max_tokens
        self._client = anthropic.Anthropic(
            api_key=api_key or os.environ.get("ANTHROPIC_API_KEY", "")
        )

    def complete(
        self,
        prompt: str,
        images: Optional[Sequence[np.ndarray]] = None,
        system: str = "",
    ) -> str:
        content: list = []
        for img in images or []:
            content.append(
                {
                    "type": "image",
                    "source": {
                        "type": "base64",
                        "media_type": "image/png",
                        "data": _encode_png_b64(img),
                    },
                }
            )
        content.append({"type": "text", "text": prompt})
        message = self._client.messages.create(
            model=self.model,
            max_tokens=self.max_tokens,
            system=system or "You are a precise robotics task evaluator.",
            messages=[{"role": "user", "content": content}],
        )
        return "".join(b.text for b in message.content if b.type == "text")


class StaticVLMClient(VLMClient):
    """Deterministic client for tests and offline dry-runs."""

    def __init__(self, reply: str = "0.0") -> None:
        self.reply = reply
        self.calls: list = []

    def complete(self, prompt, images=None, system=""):  # type: ignore[override]
        self.calls.append({"prompt": prompt, "n_images": len(images or [])})
        return self.reply


def _encode_png_b64(img: np.ndarray) -> str:
    import base64

    try:
        import cv2

        ok, buf = cv2.imencode(".png", np.asarray(img))
        if not ok:
            raise ValueError("cv2.imencode failed")
        raw = buf.tobytes()
    except ImportError:
        from PIL import Image

        bio = io.BytesIO()
        Image.fromarray(np.asarray(img).astype(np.uint8)).save(bio, format="PNG")
        raw = bio.getvalue()
    return base64.b64encode(raw).decode()
