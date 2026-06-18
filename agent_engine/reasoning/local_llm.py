"""On-device LLM reasoning backend (offline fallback).

A thin adapter around any local text generator — llama.cpp, Ollama, or a
DeepX-compiled small LLM running on the Pi's 25-TOPS NPU. Kept as an injection
point: pass a ``runner(prompt, system) -> str`` callable (or subclass and
override :meth:`complete`). ``available()`` is False until a runner is wired,
so the router uses the cloud backend by default and only falls back here when
configured and reachable offline.
"""

from __future__ import annotations

from typing import Any, Callable, Optional, Sequence


class LocalLLMBackend:
    name = "local_llm"

    def __init__(
        self,
        runner: Optional[Callable[[str, str], str]] = None,
        model_path: str = "",
    ) -> None:
        self._runner = runner
        self.model_path = model_path

    def available(self) -> bool:
        return self._runner is not None

    def complete(
        self,
        prompt: str,
        system: str = "",
        images: Optional[Sequence[Any]] = None,
    ) -> str:
        if self._runner is None:
            raise RuntimeError("local LLM backend not configured (no runner)")
        return self._runner(prompt, system)
