"""Ports (interfaces) the agent harness depends on.

The harness is a hexagonal core: it talks only to these structural
``Protocol`` ports and never imports ROS, anthropic, or the learning engine
directly. Concrete adapters live at the edges:

  * ``Perceptor``      → reads ``/agent/world_state`` (ROS) or a sim/test stub.
  * ``Reasoner``       → LangGraph + Claude tool-calling (or a scripted stub).
  * ``MemoryPort``     → ``WorkingMemory`` over ``EntityMemory`` + episode store.
  * ``Reflector``      → learning-engine evaluators (``ReflectionEvaluator``…).
  * ``VerifierPort``   → learning-engine ``InferenceVerifier`` safety gate.
  * ``ReasoningBackend`` → cloud Claude / on-device LLM / DeepX NPU.
  * ``EntityStore``    → ``omnibot_orchestration`` ``EntityMemory`` (duck-typed).

Using ``Protocol`` (not ABC subclassing) keeps adapters free of any import on
this package, which matters for the ROS nodes and learning-engine reuse.
"""

from __future__ import annotations

from typing import (
    Any,
    Dict,
    List,
    Optional,
    Protocol,
    Sequence,
    Tuple,
    runtime_checkable,
)

from .blackboard import WorldState
from .types import Goal, Plan, Reflection, ToolResult


@runtime_checkable
class Perceptor(Protocol):
    """Returns the current fused world snapshot."""

    def perceive(self) -> WorldState: ...


@runtime_checkable
class Reasoner(Protocol):
    """Decides the next :class:`Plan` for a goal given the world + memory."""

    def plan(
        self,
        goal: Goal,
        world: WorldState,
        memory_context: str,
        history: Sequence[ToolResult],
    ) -> Plan: ...


@runtime_checkable
class MemoryPort(Protocol):
    """Injects long/short-term memory into reasoning and records outcomes."""

    def context_for(self, goal: Goal, world: WorldState) -> str: ...

    def record_outcome(self, goal: Goal, reflection: Reflection) -> None: ...


@runtime_checkable
class Reflector(Protocol):
    """Self-evaluates a completed (or abandoned) attempt."""

    def reflect(
        self, goal: Goal, world: WorldState, history: Sequence[ToolResult]
    ) -> Reflection: ...


@runtime_checkable
class VerifierPort(Protocol):
    """Safety gate for low-level actuation tools.

    Returns ``(allowed, args, reason)``: ``args`` may be a clamped/modified
    copy of the request; ``allowed`` is False when no safe plan exists.
    """

    def verify(
        self, tool: str, args: Dict[str, Any], world: WorldState
    ) -> Tuple[bool, Dict[str, Any], str]: ...


@runtime_checkable
class ReasoningBackend(Protocol):
    """A place reasoning can run: cloud Claude, on-device LLM, DeepX NPU."""

    name: str

    def available(self) -> bool: ...

    def complete(
        self,
        prompt: str,
        system: str = "",
        images: Optional[Sequence[Any]] = None,
    ) -> str: ...


@runtime_checkable
class EntityStore(Protocol):
    """Structural match to ``omnibot_orchestration`` ``EntityMemory``."""

    def remember_object(
        self, object_name: str, location: str, description: str = ""
    ) -> None: ...

    def recall_object(self, object_name: str) -> Optional[Dict[str, Any]]: ...

    def get_objects_at_location(self, location: str) -> List[str]: ...

    def get_summary(self) -> str: ...
