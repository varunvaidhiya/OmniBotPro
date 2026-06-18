"""Tool/skill registry — the agent's actuators, as callable tools.

Each tool wraps a robot capability behind a name + schema + handler. In the
ROS node the handlers publish to topics (``/mission/command``,
``/control_mode``, …); in tests they record calls. The registry is pure
Python so the whole tool layer is unit-testable, and
:meth:`ToolRegistry.to_anthropic_schema` emits the tool-use schema for
Claude's tool-calling API.

Tools tagged ``low_level=True`` emit raw actuation (e.g. direct ``cmd_vel``)
and MUST pass the safety verifier before dispatch; the default high-level
tools route through ``mission_planner`` and the muxes, which already clamp.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List

from .types import ToolCall, ToolResult


@dataclass
class ToolParam:
    name: str
    description: str = ""
    required: bool = True
    type: str = "string"  # JSON-schema scalar type


@dataclass
class ToolSpec:
    name: str
    description: str
    handler: Callable[..., Any]
    params: List[ToolParam] = field(default_factory=list)
    low_level: bool = False

    def to_anthropic_schema(self) -> Dict[str, Any]:
        properties = {
            p.name: {"type": p.type, "description": p.description} for p in self.params
        }
        required = [p.name for p in self.params if p.required]
        return {
            "name": self.name,
            "description": self.description,
            "input_schema": {
                "type": "object",
                "properties": properties,
                "required": required,
            },
        }


class ToolRegistry:
    """Name → :class:`ToolSpec`, with validated dispatch."""

    def __init__(self) -> None:
        self._tools: Dict[str, ToolSpec] = {}

    def register(self, spec: ToolSpec) -> ToolSpec:
        if spec.name in self._tools:
            raise ValueError(f"tool '{spec.name}' already registered")
        self._tools[spec.name] = spec
        return spec

    def tool(
        self,
        name: str,
        description: str,
        params: List[ToolParam] | None = None,
        low_level: bool = False,
    ) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
        """Decorator form: ``@registry.tool("navigate_to", "...", [...])``."""

        def deco(fn: Callable[..., Any]) -> Callable[..., Any]:
            self.register(
                ToolSpec(name, description, fn, params or [], low_level=low_level)
            )
            return fn

        return deco

    def get(self, name: str) -> ToolSpec:
        return self._tools[name]

    def __contains__(self, name: str) -> bool:
        return name in self._tools

    def __len__(self) -> int:
        return len(self._tools)

    def names(self) -> List[str]:
        return sorted(self._tools)

    def specs(self) -> List[ToolSpec]:
        return [self._tools[n] for n in self.names()]

    def to_anthropic_schema(self) -> List[Dict[str, Any]]:
        return [s.to_anthropic_schema() for s in self.specs()]

    def dispatch(self, call: ToolCall) -> ToolResult:
        """Validate + invoke a tool call, never raising — failures are
        returned as ``ToolResult(ok=False, ...)`` so the loop can react."""
        spec = self._tools.get(call.tool)
        if spec is None:
            return ToolResult(False, f"unknown tool '{call.tool}'")
        missing = [
            p.name for p in spec.params if p.required and p.name not in call.args
        ]
        if missing:
            return ToolResult(
                False, f"tool '{call.tool}' missing required args: {missing}"
            )
        try:
            result = spec.handler(**call.args)
        except Exception as exc:  # noqa: BLE001 — surface as a result, not a crash
            return ToolResult(False, f"tool '{call.tool}' raised: {exc}")
        if isinstance(result, ToolResult):
            return result
        return ToolResult(True, "" if result is None else str(result))
