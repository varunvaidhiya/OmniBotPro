"""Build the hybrid :class:`ReasoningRouter` for a deployment.

Assembles the cloud / on-device / offline backends in preference order:
deliberation favours cloud Claude (most capable); reactive calls favour the
on-device path (lower latency, offline-safe). The :class:`EchoBackend` is
always appended last so the router never has *zero* available backends.
"""

from __future__ import annotations

from typing import Callable, Optional

from .cloud_claude import CloudClaudeBackend
from .local_llm import LocalLLMBackend
from .router import EchoBackend, ReasoningRouter


def build_reasoning_router(
    api_key: str = "",
    local_runner: Optional[Callable[[str, str], str]] = None,
    prefer_local: bool = False,
    echo_fallback: bool = True,
) -> ReasoningRouter:
    """Cloud Claude + optional on-device LLM + offline echo stub.

    ``prefer_local`` flips reactive-call preference toward the on-device
    backend (e.g. when the DeepX-hosted LLM should handle fast decisions and
    the cloud is reserved for deep deliberation).
    """
    backends = [CloudClaudeBackend(api_key=api_key), LocalLLMBackend(local_runner)]
    if echo_fallback:
        backends.append(EchoBackend())

    deliberate = ["cloud_claude", "local_llm", "echo"]
    reactive = (
        ["local_llm", "echo", "cloud_claude"]
        if prefer_local
        else ["local_llm", "cloud_claude", "echo"]
    )
    return ReasoningRouter(
        backends, deliberate_order=deliberate, reactive_order=reactive
    )
