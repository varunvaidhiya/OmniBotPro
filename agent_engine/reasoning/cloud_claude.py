"""Cloud Claude reasoning backend (lazy ``anthropic`` import).

Primary backend for deliberation, recovery and reflection. Mirrors the model
and env-var conventions already used in ``omnibot_orchestration``
(``ANTHROPIC_API_KEY``, ``claude-sonnet-4-6``). ``available()`` is honest:
False when no key or the ``anthropic`` package is missing, so the
:class:`~agent_engine.reasoning.router.ReasoningRouter` transparently falls
back to an on-device backend.
"""

from __future__ import annotations

import os
from typing import Any, List, Optional, Sequence


class CloudClaudeBackend:
    name = "cloud_claude"
    DEFAULT_MODEL = "claude-sonnet-4-6"

    def __init__(
        self,
        api_key: str = "",
        model: str = "",
        max_tokens: int = 1024,
    ) -> None:
        self._api_key = api_key or os.environ.get("ANTHROPIC_API_KEY", "")
        self._model = model or self.DEFAULT_MODEL
        self._max_tokens = max_tokens
        self._client: Any = None  # lazily constructed

    def available(self) -> bool:
        if not self._api_key:
            return False
        try:
            import anthropic  # noqa: F401
        except ImportError:
            return False
        return True

    def _ensure_client(self) -> Any:
        if self._client is None:
            import anthropic

            self._client = anthropic.Anthropic(api_key=self._api_key)
        return self._client

    def complete(
        self,
        prompt: str,
        system: str = "",
        images: Optional[Sequence[Any]] = None,
    ) -> str:
        client = self._ensure_client()
        content: List[dict] = [{"type": "text", "text": prompt}]
        for block in self._encode_images(images or []):
            content.append(block)
        resp = client.messages.create(
            model=self._model,
            max_tokens=self._max_tokens,
            system=system or "You are OmniBot's reasoning module.",
            messages=[{"role": "user", "content": content}],
        )
        parts = [b.text for b in resp.content if getattr(b, "type", "") == "text"]
        return "".join(parts).strip()

    @staticmethod
    def _encode_images(images: Sequence[Any]) -> List[dict]:
        """Encode numpy HxWx3 frames to base64 image blocks (best-effort).

        Requires Pillow; silently drops frames if it is unavailable so a
        text-only completion still succeeds.
        """
        if not images:
            return []
        try:
            import base64
            import io

            from PIL import Image  # type: ignore
        except ImportError:
            return []
        blocks: List[dict] = []
        for frame in images:
            try:
                img = Image.fromarray(frame)
                buf = io.BytesIO()
                img.save(buf, format="JPEG")
                b64 = base64.b64encode(buf.getvalue()).decode("ascii")
                blocks.append(
                    {
                        "type": "image",
                        "source": {
                            "type": "base64",
                            "media_type": "image/jpeg",
                            "data": b64,
                        },
                    }
                )
            except Exception:  # noqa: BLE001 — a bad frame must not break reasoning
                continue
        return blocks
