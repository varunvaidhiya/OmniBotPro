"""Shared test stubs for the agent harness — fake ports and a tool log."""

from __future__ import annotations

from typing import Any, Dict, List, Optional

from agent_engine.core.blackboard import DetectedObject, WorldState
from agent_engine.core.tools import ToolParam, ToolRegistry, ToolSpec
from agent_engine.core.types import ToolResult


class FakePerceptor:
    """Returns a fixed (or settable) world snapshot."""

    def __init__(self, world: Optional[WorldState] = None) -> None:
        self._world = world or WorldState()
        self.calls = 0

    def set(self, world: WorldState) -> None:
        self._world = world

    def perceive(self) -> WorldState:
        self.calls += 1
        return self._world


class FakeEntityStore:
    """In-memory structural match to ``EntityMemory``."""

    def __init__(self) -> None:
        self.objects: Dict[str, Dict[str, Any]] = {}

    def remember_object(
        self, object_name: str, location: str, description: str = ""
    ) -> None:
        self.objects[object_name] = {
            "last_seen_location": location,
            "description": description,
        }

    def recall_object(self, object_name: str) -> Optional[Dict[str, Any]]:
        return self.objects.get(object_name)

    def get_objects_at_location(self, location: str) -> List[str]:
        return [
            n for n, v in self.objects.items() if v["last_seen_location"] == location
        ]

    def get_summary(self) -> str:
        if not self.objects:
            return "No objects remembered yet."
        return "\n".join(
            f"- {n}: last seen at {v['last_seen_location']}"
            for n, v in self.objects.items()
        )


class BlockingVerifier:
    """VerifierPort stub: allow/deny everything, recording what it saw."""

    def __init__(self, allow: bool = True) -> None:
        self.allow = allow
        self.seen: List[str] = []

    def verify(self, tool: str, args: Dict[str, Any], world: WorldState):
        self.seen.append(tool)
        reason = "ok" if self.allow else "unsafe"
        return self.allow, args, reason


def make_world(objects: Optional[List[DetectedObject]] = None, **kw: Any) -> WorldState:
    return WorldState(detected_objects=objects or [], **kw)


def make_registry(log: List[str]) -> ToolRegistry:
    """Registry with representative high- and low-level tools that append to
    ``log`` so tests can assert what was actuated."""
    reg = ToolRegistry()

    reg.register(
        ToolSpec(
            "navigate_to",
            "Drive to a named location via Nav2.",
            lambda location: (
                log.append(f"navigate_to:{location}")
                or ToolResult(True, f"navigating to {location}")
            ),
            [ToolParam("location", "named location")],
        )
    )
    reg.register(
        ToolSpec(
            "cancel_mission",
            "Abort the current mission.",
            lambda: log.append("cancel_mission") or ToolResult(True, "cancelled"),
        )
    )
    reg.register(
        ToolSpec(
            "ask_human",
            "Ask the operator a question.",
            lambda question: (
                log.append(f"ask_human:{question}") or ToolResult(True, "asked")
            ),
            [ToolParam("question", "question text")],
        )
    )
    reg.register(
        ToolSpec(
            "drive",
            "Low-level base velocity command.",
            lambda vx: log.append(f"drive:{vx}") or ToolResult(True, f"drive {vx}"),
            [ToolParam("vx", "linear x", type="number")],
            low_level=True,
        )
    )
    return reg
