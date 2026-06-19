"""Pure-Python core of the agent harness (no ROS / no anthropic).

Importing ``agent_engine.core`` pulls in only the loop, value types,
blackboard and tool registry — everything needed to drive and test the
deliberative cycle. Heavier edges (cloud Claude, ROS adapters, learning
engine) import lazily where they are used.
"""

from __future__ import annotations

from .blackboard import DetectedObject, WorldState
from .harness import AgentHarness
from .tools import ToolParam, ToolRegistry, ToolSpec
from .types import (
    Goal,
    GoalStatus,
    HarnessPhase,
    Plan,
    Reflection,
    TickReport,
    ToolCall,
    ToolResult,
)

__all__ = [
    "AgentHarness",
    "WorldState",
    "DetectedObject",
    "ToolRegistry",
    "ToolSpec",
    "ToolParam",
    "Goal",
    "GoalStatus",
    "Plan",
    "ToolCall",
    "ToolResult",
    "Reflection",
    "HarnessPhase",
    "TickReport",
]
