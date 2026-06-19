"""AgentHarness — the continuous perceive→reason→act→reflect loop.

This is the deliberative "brain" that turns OmniBot from a one-shot command
executor into a continuously-operating agent. It runs a small state machine,
one transition per :meth:`tick` (so a ROS timer or a test can drive it):

    IDLE ──goal──► PERCEIVE ──► PLAN ──► ACT ──► MONITOR ──┐
      ▲                            │                       │
      │                            ├─ goal_complete/give_up┤
      │                            ├─ ask_human ─► WAIT_HUMAN
      │                            │                       │ (budget left)
      └── REMEMBER ◄── REFLECT ◄───┴──────────◄────────────┘ loop to PERCEIVE

Design choices:
  * One phase per ``tick`` keeps each step cheap and the machine observable;
    the expensive work (LLM planning, tool I/O) is isolated to PLAN/ACT.
  * MONITOR loops back to PERCEIVE so the agent re-grounds in fresh state
    every cycle — the heart of continuous, closed-loop behaviour. A per-goal
    step budget bounds the loop; the reasoner ends it via ``goal_complete``.
  * Emergency-stop and human-checkpoint are first-class interrupts.
  * Every dependency is an injected port (see ``core.interfaces``) so the
    loop is fully unit-testable with stubs.
"""

from __future__ import annotations

from collections import deque
from typing import Callable, Deque, List, Optional, Union

from .blackboard import WorldState
from .interfaces import (
    MemoryPort,
    Perceptor,
    Reasoner,
    Reflector,
    VerifierPort,
)
from .tools import ToolRegistry
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

Logger = Callable[[str], None]


class AgentHarness:
    """The deliberative control loop. Wire ports in; call :meth:`tick`."""

    def __init__(
        self,
        perceptor: Perceptor,
        reasoner: Reasoner,
        tools: ToolRegistry,
        *,
        memory: Optional[MemoryPort] = None,
        reflector: Optional[Reflector] = None,
        verifier: Optional[VerifierPort] = None,
        max_steps_per_goal: int = 12,
        logger: Optional[Logger] = None,
    ) -> None:
        self.perceptor = perceptor
        self.reasoner = reasoner
        self.tools = tools
        self.memory = memory
        self.reflector = reflector
        self.verifier = verifier
        self.max_steps_per_goal = max_steps_per_goal
        self._log = logger or (lambda _m: None)

        self.phase: HarnessPhase = HarnessPhase.IDLE
        self.current_goal: Optional[Goal] = None
        self.world: Optional[WorldState] = None
        self._queue: Deque[Goal] = deque()
        self._plan: Optional[Plan] = None
        self._history: List[ToolResult] = []
        self._step = 0
        self._estop = False
        self._human_reply: Optional[str] = None
        self.last_reflection: Optional[Reflection] = None

    # ------------------------------------------------------------------
    # Control surface (called from ROS subscriptions / operators)
    # ------------------------------------------------------------------
    def submit_goal(self, goal: Union[Goal, str]) -> Goal:
        g = Goal(text=goal) if isinstance(goal, str) else goal
        self._queue.append(g)
        self._log(f"goal queued: {g.text!r} ({g.id})")
        return g

    @property
    def pending_goals(self) -> int:
        return len(self._queue)

    def set_emergency_stop(self, engaged: bool) -> None:
        self._estop = engaged
        self._log(f"emergency stop {'ENGAGED' if engaged else 'cleared'}")

    def cancel_current(self, reason: str = "operator cancel") -> None:
        if self.current_goal is None:
            return
        if "cancel_mission" in self.tools:
            self.tools.dispatch(ToolCall("cancel_mission", {}))
        self.current_goal.status = GoalStatus.CANCELLED
        self._log(f"goal {self.current_goal.id} cancelled: {reason}")
        self._end_goal()
        self.phase = HarnessPhase.IDLE

    def provide_human_response(self, text: str) -> None:
        """Answer an outstanding ``ask_human`` checkpoint; loop resumes."""
        self._human_reply = text
        self._log(f"human response received: {text!r}")

    # ------------------------------------------------------------------
    # The loop
    # ------------------------------------------------------------------
    def tick(self) -> TickReport:
        ran = self.phase
        # Global interrupt: e-stop overrides everything except idling.
        if self._estop and self.phase is not HarnessPhase.IDLE:
            self._abort_current("emergency stop")
            self.phase = HarnessPhase.IDLE
            return self._report(ran, HarnessPhase.IDLE, "emergency stop active")

        handler = {
            HarnessPhase.IDLE: self._tick_idle,
            HarnessPhase.PERCEIVE: self._tick_perceive,
            HarnessPhase.PLAN: self._tick_plan,
            HarnessPhase.ACT: self._tick_act,
            HarnessPhase.MONITOR: self._tick_monitor,
            HarnessPhase.REFLECT: self._tick_reflect,
            HarnessPhase.REMEMBER: self._tick_remember,
            HarnessPhase.WAIT_HUMAN: self._tick_wait_human,
        }[self.phase]

        nxt, detail, results = handler()
        self.phase = nxt
        return self._report(ran, nxt, detail, results)

    def run_until_idle(self, max_ticks: int = 200) -> List[TickReport]:
        """Drive the machine until it idles or blocks on a human.

        Convenience for sim smoke-tests and unit tests; the ROS node calls
        :meth:`tick` from a timer instead.
        """
        reports: List[TickReport] = []
        for _ in range(max_ticks):
            if self.phase is HarnessPhase.WAIT_HUMAN and self._human_reply is None:
                break
            if self._estop and self.phase is HarnessPhase.IDLE:
                break  # estopped: queued goals wait until estop clears
            if (
                self.phase is HarnessPhase.IDLE
                and not self._queue
                and self.current_goal is None
            ):
                break
            reports.append(self.tick())
        return reports

    # ------------------------------------------------------------------
    # Phase handlers — each returns (next_phase, detail, tool_results)
    # ------------------------------------------------------------------
    def _tick_idle(self):
        if self._estop or not self._queue:
            return HarnessPhase.IDLE, "idle", []
        self.current_goal = self._queue.popleft()
        self.current_goal.status = GoalStatus.ACTIVE
        self._history = []
        self._plan = None
        self._step = 0
        self.last_reflection = None
        self._log(f"starting goal {self.current_goal.id}: {self.current_goal.text!r}")
        return HarnessPhase.PERCEIVE, f"begin {self.current_goal.text!r}", []

    def _tick_perceive(self):
        self.world = self.perceptor.perceive()
        if self.world.emergency_stop:
            self._estop = True
            self._abort_current("emergency stop in world state")
            return HarnessPhase.IDLE, "estop from world state", []
        return HarnessPhase.PLAN, "perceived world state", []

    def _tick_plan(self):
        assert self.current_goal is not None and self.world is not None
        context = (
            self.memory.context_for(self.current_goal, self.world)
            if self.memory
            else ""
        )
        plan = self.reasoner.plan(
            self.current_goal, self.world, context, tuple(self._history)
        )
        self._plan = plan

        if plan.ask_human:
            if "ask_human" in self.tools:
                self.tools.dispatch(ToolCall("ask_human", {"question": plan.ask_human}))
            return HarnessPhase.WAIT_HUMAN, f"ask human: {plan.ask_human}", []
        if plan.goal_complete:
            return HarnessPhase.REFLECT, "reasoner: goal complete", []
        if plan.give_up:
            return HarnessPhase.REFLECT, "reasoner: give up", []
        return HarnessPhase.ACT, plan.rationale or "execute plan", []

    def _tick_act(self):
        results: List[ToolResult] = []
        plan = self._plan
        if plan is not None:
            for call in plan.calls:
                results.append(self._dispatch_guarded(call))
        self._history.extend(results)
        self._step += 1
        return HarnessPhase.MONITOR, f"executed {len(results)} call(s)", results

    def _tick_monitor(self):
        if self._step >= self.max_steps_per_goal:
            return HarnessPhase.REFLECT, "step budget exhausted", []
        # Continuous loop: re-perceive and plan again with fresh state.
        return HarnessPhase.PERCEIVE, "re-perceive", []

    def _tick_reflect(self):
        assert self.current_goal is not None and self.world is not None
        if self.reflector is not None:
            refl = self.reflector.reflect(
                self.current_goal, self.world, tuple(self._history)
            )
        else:
            refl = self._heuristic_reflection()
        self.last_reflection = refl
        self.current_goal.status = (
            GoalStatus.SUCCEEDED if refl.success else GoalStatus.FAILED
        )
        return HarnessPhase.REMEMBER, f"reflection success={refl.success}", []

    def _tick_remember(self):
        if self.memory is not None and self.current_goal and self.last_reflection:
            self.memory.record_outcome(self.current_goal, self.last_reflection)
        self._end_goal()
        return HarnessPhase.IDLE, "goal done", []

    def _tick_wait_human(self):
        if self._human_reply is None:
            return HarnessPhase.WAIT_HUMAN, "awaiting human", []
        self._history.append(ToolResult(True, f"human said: {self._human_reply}"))
        self._human_reply = None
        return HarnessPhase.PERCEIVE, "resumed after human reply", []

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------
    def _dispatch_guarded(self, call: ToolCall) -> ToolResult:
        """Dispatch a tool call, gating low-level actuation through the
        safety verifier when one is configured."""
        spec = self.tools._tools.get(call.tool)
        if spec is not None and spec.low_level and self.verifier is not None:
            allowed, safe_args, reason = self.verifier.verify(
                call.tool, dict(call.args), self.world or WorldState()
            )
            if not allowed:
                self._log(f"verifier blocked {call.tool}: {reason}")
                return ToolResult(False, f"blocked by safety gate: {reason}")
            call = ToolCall(call.tool, safe_args)
        return self.tools.dispatch(call)

    def _heuristic_reflection(self) -> Reflection:
        plan = self._plan
        if plan is not None and plan.give_up:
            return Reflection(False, 0.2, "Reasoner abandoned the goal.")
        failures = [r for r in self._history if not r.ok]
        budget_hit = self._step >= self.max_steps_per_goal
        success = not failures and not budget_hit
        summary = (
            "Completed without tool failures."
            if success
            else f"{len(failures)} failure(s); budget_hit={budget_hit}."
        )
        return Reflection(success, 0.8 if success else 0.3, summary)

    def _abort_current(self, reason: str) -> None:
        if self.current_goal is not None:
            if "cancel_mission" in self.tools:
                self.tools.dispatch(ToolCall("cancel_mission", {}))
            self.current_goal.status = GoalStatus.CANCELLED
            self._log(f"goal {self.current_goal.id} aborted: {reason}")
        self._end_goal()

    def _end_goal(self) -> None:
        self.current_goal = None
        self._plan = None
        self._history = []
        self._step = 0

    def _report(
        self,
        ran: HarnessPhase,
        nxt: HarnessPhase,
        detail: str,
        results: Optional[List[ToolResult]] = None,
    ) -> TickReport:
        return TickReport(
            phase=ran,
            next_phase=nxt,
            goal_id=self.current_goal.id if self.current_goal else None,
            detail=detail,
            tool_results=results or [],
        )
