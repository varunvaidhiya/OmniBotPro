"""ScriptedReasoner — a deterministic :class:`Reasoner` for tests and sim.

Plays back a fixed sequence of :class:`Plan` objects (or delegates to a
callable). Lets the harness loop be exercised end-to-end without an LLM, and
gives Phase-1 sim smoke-tests a predictable driver before the LangGraph
tool-calling reasoner is wired in.
"""

from __future__ import annotations

from typing import Callable, List, Sequence, Union

from ..core.blackboard import WorldState
from ..core.types import Goal, Plan, ToolResult

PlanFn = Callable[[Goal, WorldState, str, Sequence[ToolResult]], Plan]


class ScriptedReasoner:
    def __init__(self, plans: Union[Sequence[Plan], PlanFn]) -> None:
        self._fn: PlanFn | None = plans if callable(plans) else None
        self._plans: List[Plan] = [] if callable(plans) else list(plans)
        self._i = 0

    def plan(
        self,
        goal: Goal,
        world: WorldState,
        memory_context: str,
        history: Sequence[ToolResult],
    ) -> Plan:
        if self._fn is not None:
            return self._fn(goal, world, memory_context, history)
        if self._i < len(self._plans):
            plan = self._plans[self._i]
            self._i += 1
            return plan
        # Out of scripted steps — declare completion so the loop terminates.
        return Plan(rationale="scripted: complete", goal_complete=True)

    def reset(self) -> None:
        self._i = 0
