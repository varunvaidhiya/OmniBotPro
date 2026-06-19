"""ClaudeToolCallingReasoner — the LLM brain for the PLAN phase.

Drives tool-calling through the :class:`ReasoningRouter` (cloud Claude when
reachable, on-device LLM otherwise), grounded in the fused world state plus
injected memory. It uses a backend-agnostic structured-output protocol (the
model replies with JSON) rather than a single provider's native tool-use API,
so the *same* reasoner works across cloud Claude, a local LLM and the
offline echo stub — and stays unit-testable without any network.

The available tools (names, descriptions, parameters) come straight from the
harness :class:`ToolRegistry` via ``to_anthropic_schema()``, so the model only
ever sees the actuators that are actually wired.
"""

from __future__ import annotations

import json
import re
from typing import Sequence

from ..core.blackboard import WorldState
from ..core.tools import ToolRegistry
from ..core.types import Goal, Plan, ToolCall, ToolResult
from ..reasoning.router import ReasoningRouter

_SYSTEM = """You are OmniBot's planning module — the deliberative brain of a \
mobile-manipulation robot. Each cycle you receive the fused world state, \
relevant memory, and the result of your previous actions, and you decide the \
next step toward the goal.

You may ONLY act through the tools listed below. Respond with a SINGLE JSON \
object and nothing else:
{{"rationale": "<one sentence on your reasoning>",
  "tool_calls": [{{"tool": "<name>", "args": {{...}}}}],
  "goal_complete": <true if the goal is fully achieved>,
  "give_up": <true if the goal is impossible/unsafe to continue>,
  "ask_human": "<question, or empty string>"}}

Rules:
- Prefer one tool call per cycle; you will re-plan after seeing its result.
- Set goal_complete=true (with empty tool_calls) once the goal is achieved.
- Use ask_human only when genuinely blocked by ambiguity.

Available tools (JSON schema):
{tools}"""


class ClaudeToolCallingReasoner:
    def __init__(
        self,
        router: ReasoningRouter,
        tools: ToolRegistry,
        max_history: int = 6,
        call_kind: str = "deliberate",
    ) -> None:
        self.router = router
        self.tools = tools
        self.max_history = max_history
        self.call_kind = call_kind

    def plan(
        self,
        goal: Goal,
        world: WorldState,
        memory_context: str,
        history: Sequence[ToolResult],
    ) -> Plan:
        system = _SYSTEM.format(
            tools=json.dumps(self.tools.to_anthropic_schema(), indent=1)
        )
        reply = self.router.complete(
            self._user_prompt(goal, world, memory_context, history),
            system=system,
            kind=self.call_kind,
        )
        return self._parse(reply)

    # -- prompt + parse ----------------------------------------------------
    def _user_prompt(
        self,
        goal: Goal,
        world: WorldState,
        memory_context: str,
        history: Sequence[ToolResult],
    ) -> str:
        parts = [f"GOAL: {goal.text}", "", "WORLD STATE:", world.summarize()]
        if memory_context:
            parts += ["", "MEMORY:", memory_context]
        if history:
            recent = history[-self.max_history :]
            parts += ["", "RESULTS OF YOUR RECENT ACTIONS:"]
            parts += [f"- [{'ok' if r.ok else 'FAILED'}] {r.output}" for r in recent]
        parts += ["", "Respond with the JSON object now."]
        return "\n".join(parts)

    def _parse(self, reply: str) -> Plan:
        doc = _extract_json(reply)
        calls = []
        for c in doc.get("tool_calls") or []:
            name = c.get("tool")
            if isinstance(name, str) and name:
                args = c.get("args") or {}
                calls.append(ToolCall(name, args if isinstance(args, dict) else {}))
        return Plan(
            rationale=str(doc.get("rationale", "")),
            calls=calls,
            goal_complete=bool(doc.get("goal_complete", False)),
            give_up=bool(doc.get("give_up", False)),
            ask_human=str(doc.get("ask_human", "") or ""),
        )


def _extract_json(reply: str) -> dict:
    """Best-effort: parse the first JSON object in the reply. On failure,
    return a give-up plan doc so the harness reflects rather than looping."""
    try:
        match = re.search(r"\{.*\}", reply, re.DOTALL)
        if match:
            doc = json.loads(match.group())
            if isinstance(doc, dict):
                return doc
    except (ValueError, TypeError):
        pass
    return {"rationale": "unparseable model reply", "give_up": True}
