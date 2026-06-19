"""ReasoningRouter — picks where each LLM/VLM call runs (hybrid brain).

OmniBot's reasoning can run in three places:
  * cloud Claude (most capable — deep planning, recovery, reflection),
  * an on-device LLM (offline fallback / fast reactive decisions),
  * the Pi's DeepX NPU (always-on accelerated perception & local scoring).

The router holds an ordered list of backends and, per call ``kind``, returns
the first *available* one — so the agent degrades gracefully when the network
or a backend is down. ``deliberate`` calls prefer the cloud; ``reactive``
calls prefer on-device. :class:`EchoBackend` is a dependency-free backend used
in tests and as a last-resort offline stub.
"""

from __future__ import annotations

from typing import Any, Callable, Dict, List, Optional, Sequence

from ..core.interfaces import ReasoningBackend


class EchoBackend:
    """Always-available, dependency-free backend.

    With no ``responder`` it returns an empty string; tests inject a callable
    to script deterministic completions.
    """

    name = "echo"

    def __init__(self, responder: Optional[Callable[[str, str], str]] = None) -> None:
        self._responder = responder

    def available(self) -> bool:
        return True

    def complete(
        self,
        prompt: str,
        system: str = "",
        images: Optional[Sequence[Any]] = None,
    ) -> str:
        return self._responder(prompt, system) if self._responder else ""


class ReasoningRouter:
    """Routes completions across backends by availability and call kind."""

    def __init__(
        self,
        backends: Sequence[ReasoningBackend],
        deliberate_order: Optional[Sequence[str]] = None,
        reactive_order: Optional[Sequence[str]] = None,
    ) -> None:
        if not backends:
            raise ValueError("ReasoningRouter needs at least one backend")
        self._backends: Dict[str, ReasoningBackend] = {b.name: b for b in backends}
        self._default_order: List[str] = [b.name for b in backends]
        self._deliberate = list(deliberate_order or self._default_order)
        self._reactive = list(reactive_order or self._default_order)

    def select(self, kind: str = "deliberate") -> Optional[ReasoningBackend]:
        order = self._reactive if kind == "reactive" else self._deliberate
        for name in order:
            b = self._backends.get(name)
            if b is not None and b.available():
                return b
        # Fall back to any available backend regardless of preference.
        for name in self._default_order:
            if self._backends[name].available():
                return self._backends[name]
        return None

    def complete(
        self,
        prompt: str,
        system: str = "",
        images: Optional[Sequence[Any]] = None,
        kind: str = "deliberate",
    ) -> str:
        backend = self.select(kind)
        if backend is None:
            raise RuntimeError("no reasoning backend available")
        return backend.complete(prompt, system, images)

    def status(self) -> Dict[str, bool]:
        """Availability of each backend (for ``/agent/status`` diagnostics)."""
        return {name: b.available() for name, b in self._backends.items()}
