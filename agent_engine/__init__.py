"""OmniBot agent harness — the deliberative "brain" loop.

Turns OmniBot from a one-shot command executor into a continuously-operating
physical agent: a persistent perceive → reason → verify → act → monitor →
reflect → remember cycle that grounds every decision in a fused world state
and long-term memory, gates actuation through the safety verifier, and closes
the loop back into the learning engine.

Architecture-first, mirroring ``learning_engine``: the pure-Python core
(``agent_engine.core``) depends only on numpy and is fully unit-testable;
heavier edges (cloud Claude, ROS adapters, learning-engine reuse) import
lazily where used. See ARCHITECTURE.md.
"""

from __future__ import annotations

from . import core
from .core import (  # noqa: F401 — convenience re-exports
    AgentHarness,
    Goal,
    HarnessPhase,
    Plan,
    Reflection,
    ToolCall,
    ToolRegistry,
    ToolResult,
    WorldState,
)

__version__ = "0.1.0"
__all__ = [
    "core",
    "AgentHarness",
    "WorldState",
    "Goal",
    "Plan",
    "ToolCall",
    "ToolResult",
    "ToolRegistry",
    "Reflection",
    "HarnessPhase",
    "__version__",
]
