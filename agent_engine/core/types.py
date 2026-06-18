"""Core value types for the OmniBot agent harness.

These are the *lingua franca* between the harness and its ports: a
:class:`Reasoner` turns a :class:`Goal` + world snapshot into a :class:`Plan`
(an ordered list of :class:`ToolCall`), the harness dispatches each call and
collects :class:`ToolResult` objects, and a :class:`Reflector` summarises the
attempt as a :class:`Reflection`.

Pure stdlib — no numpy/ROS — so the whole control loop is unit-testable.
"""

from __future__ import annotations

import time
import uuid
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional


class GoalStatus(str, Enum):
    PENDING = "pending"
    ACTIVE = "active"
    SUCCEEDED = "succeeded"
    FAILED = "failed"
    CANCELLED = "cancelled"


@dataclass
class Goal:
    """A high-level objective the agent should pursue.

    ``text`` is the natural-language instruction; ``structured`` optionally
    carries a pre-parsed form (e.g. ``{"navigate": "kitchen", "vla": "..."}``)
    when the goal arrives on ``/agent/goal`` rather than ``/ai/command``.
    """

    text: str
    structured: Optional[Dict[str, Any]] = None
    id: str = field(default_factory=lambda: uuid.uuid4().hex[:12])
    created_at: float = field(default_factory=time.time)
    status: GoalStatus = GoalStatus.PENDING


@dataclass
class ToolCall:
    """A single requested invocation of a registered tool."""

    tool: str
    args: Dict[str, Any] = field(default_factory=dict)


@dataclass
class ToolResult:
    """Outcome of dispatching a :class:`ToolCall`."""

    ok: bool
    output: str = ""
    data: Dict[str, Any] = field(default_factory=dict)


@dataclass
class Plan:
    """The reasoner's decision for one deliberation cycle.

    Exactly one intent dominates per cycle:
      * ``goal_complete`` — the reasoner believes the goal is satisfied.
      * ``give_up`` — the goal is unreachable; abandon (and reflect on why).
      * ``ask_human`` — non-empty question; pause for an operator response.
      * otherwise — execute ``calls`` then re-perceive and plan again.
    """

    rationale: str = ""
    calls: List[ToolCall] = field(default_factory=list)
    goal_complete: bool = False
    give_up: bool = False
    ask_human: str = ""


@dataclass
class Reflection:
    """Post-attempt self-assessment, produced at episode end."""

    success: bool
    confidence: float = 0.0  # [0, 1]
    summary: str = ""
    analysis: str = ""
    # object name -> location, written back to long-term memory
    learned_objects: Dict[str, str] = field(default_factory=dict)


class HarnessPhase(str, Enum):
    """States of the deliberative loop's state machine."""

    IDLE = "idle"
    PERCEIVE = "perceive"
    PLAN = "plan"
    ACT = "act"
    MONITOR = "monitor"
    REFLECT = "reflect"
    REMEMBER = "remember"
    WAIT_HUMAN = "wait_human"


@dataclass
class TickReport:
    """What one :meth:`AgentHarness.tick` did — handy for logging/tests."""

    phase: HarnessPhase  # the phase that ran this tick
    next_phase: HarnessPhase
    goal_id: Optional[str] = None
    detail: str = ""
    tool_results: List[ToolResult] = field(default_factory=list)
