"""End-to-end tests for the AgentHarness loop, driven by stub ports."""

from __future__ import annotations

import unittest

from agent_engine.core.harness import AgentHarness
from agent_engine.core.types import Goal, GoalStatus, HarnessPhase, Plan, ToolCall
from agent_engine.memory.working_memory import WorkingMemory
from agent_engine.reasoners.scripted import ScriptedReasoner

from .helpers import BlockingVerifier, FakePerceptor, make_registry, make_world


def build(reasoner, *, log=None, **kw):
    log = [] if log is None else log
    harness = AgentHarness(
        FakePerceptor(make_world()),
        reasoner,
        make_registry(log),
        **kw,
    )
    return harness, log


class TestHarnessLoop(unittest.TestCase):
    def test_idle_with_no_goal_stays_idle(self):
        harness, log = build(ScriptedReasoner([]))
        reports = harness.run_until_idle()
        self.assertEqual(reports, [])
        self.assertIs(harness.phase, HarnessPhase.IDLE)
        self.assertEqual(log, [])

    def test_happy_path_navigate_then_complete(self):
        plans = [
            Plan(calls=[ToolCall("navigate_to", {"location": "kitchen"})]),
            Plan(goal_complete=True),
        ]
        harness, log = build(ScriptedReasoner(plans))
        goal = harness.submit_goal("go to the kitchen")
        harness.run_until_idle()

        self.assertEqual(log, ["navigate_to:kitchen"])
        self.assertEqual(goal.status, GoalStatus.SUCCEEDED)
        self.assertIs(harness.phase, HarnessPhase.IDLE)
        self.assertIsNotNone(harness.last_reflection)
        self.assertTrue(harness.last_reflection.success)

    def test_phase_sequence(self):
        plans = [
            Plan(calls=[ToolCall("navigate_to", {"location": "x"})]),
            Plan(goal_complete=True),
        ]
        harness, _ = build(ScriptedReasoner(plans))
        harness.submit_goal("g")
        seq = [r.phase for r in harness.run_until_idle()]
        self.assertEqual(
            seq,
            [
                HarnessPhase.IDLE,
                HarnessPhase.PERCEIVE,
                HarnessPhase.PLAN,
                HarnessPhase.ACT,
                HarnessPhase.MONITOR,
                HarnessPhase.PERCEIVE,
                HarnessPhase.PLAN,
                HarnessPhase.REFLECT,
                HarnessPhase.REMEMBER,
            ],
        )

    def test_memory_records_outcome(self):
        mem = WorkingMemory()
        plans = [Plan(goal_complete=True)]
        harness, _ = build(ScriptedReasoner(plans), memory=mem)
        harness.submit_goal("inspect the room")
        harness.run_until_idle()
        ctx = mem.context_for(Goal("next"), make_world())
        self.assertIn("inspect the room", ctx)

    def test_step_budget_bounds_unbounded_reasoner(self):
        # Reasoner never completes -> budget must stop it.
        reasoner = ScriptedReasoner(
            lambda g, w, c, h: Plan(calls=[ToolCall("navigate_to", {"location": "x"})])
        )
        harness, log = build(reasoner, max_steps_per_goal=3)
        goal = harness.submit_goal("loop forever")
        harness.run_until_idle()
        self.assertEqual(len(log), 3)
        self.assertEqual(goal.status, GoalStatus.FAILED)

    def test_emergency_stop_aborts_active_goal(self):
        reasoner = ScriptedReasoner(
            lambda g, w, c, h: Plan(calls=[ToolCall("navigate_to", {"location": "x"})])
        )
        harness, log = build(reasoner)
        goal = harness.submit_goal("drive around")
        harness.tick()  # IDLE -> PERCEIVE (goal now active)
        harness.set_emergency_stop(True)
        harness.tick()  # estop interrupt -> abort
        self.assertIs(harness.phase, HarnessPhase.IDLE)
        self.assertEqual(goal.status, GoalStatus.CANCELLED)
        self.assertIn("cancel_mission", log)
        # Estopped harness will not start queued goals.
        harness.submit_goal("another")
        harness.run_until_idle()
        self.assertIs(harness.phase, HarnessPhase.IDLE)

    def test_low_level_action_blocked_by_verifier(self):
        plans = [Plan(calls=[ToolCall("drive", {"vx": 0.1})]), Plan(goal_complete=True)]
        verifier = BlockingVerifier(allow=False)
        harness, log = build(ScriptedReasoner(plans), verifier=verifier)
        harness.submit_goal("nudge forward")
        reports = harness.run_until_idle()
        self.assertNotIn("drive:0.1", log)  # never actuated
        self.assertIn("drive", verifier.seen)
        # The blocked result is recorded in the act report.
        act = next(r for r in reports if r.phase is HarnessPhase.ACT)
        self.assertFalse(act.tool_results[0].ok)
        self.assertIn("safety gate", act.tool_results[0].output)

    def test_low_level_action_allowed_by_verifier(self):
        plans = [Plan(calls=[ToolCall("drive", {"vx": 0.1})]), Plan(goal_complete=True)]
        harness, log = build(ScriptedReasoner(plans), verifier=BlockingVerifier(True))
        harness.submit_goal("nudge forward")
        harness.run_until_idle()
        self.assertIn("drive:0.1", log)

    def test_ask_human_pauses_then_resumes(self):
        plans = [Plan(ask_human="which cup?"), Plan(goal_complete=True)]
        harness, log = build(ScriptedReasoner(plans))
        goal = harness.submit_goal("fetch a cup")
        harness.run_until_idle()
        self.assertIs(harness.phase, HarnessPhase.WAIT_HUMAN)
        self.assertIn("ask_human:which cup?", log)

        harness.provide_human_response("the red one")
        harness.run_until_idle()
        self.assertIs(harness.phase, HarnessPhase.IDLE)
        self.assertEqual(goal.status, GoalStatus.SUCCEEDED)

    def test_continuous_operation_across_multiple_goals(self):
        # The loop returns to IDLE and picks up the next goal — continuous.
        reasoner = ScriptedReasoner(
            lambda g, w, c, h: Plan(
                calls=[ToolCall("navigate_to", {"location": g.text})],
                goal_complete=len(h) > 0,
            )
        )
        harness, log = build(reasoner)
        harness.submit_goal("alpha")
        harness.submit_goal("bravo")
        harness.run_until_idle()
        self.assertEqual(log, ["navigate_to:alpha", "navigate_to:bravo"])
        self.assertEqual(harness.pending_goals, 0)


if __name__ == "__main__":
    unittest.main()
