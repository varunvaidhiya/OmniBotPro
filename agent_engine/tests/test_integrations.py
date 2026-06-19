"""Tests for the learning_engine adapters (verifier, reflector, memory, learn).

These exercise real ``learning_engine`` components (numpy-only path), proving
the harness ports are correctly wired to the existing safety/reflection/learning
machinery.
"""

from __future__ import annotations

import tempfile
import unittest

from agent_engine.core.harness import AgentHarness
from agent_engine.core.tools import ToolParam, ToolRegistry, ToolSpec
from agent_engine.core.types import Goal, Plan, ToolCall, ToolResult
from agent_engine.integrations.learning_engine import (
    ContinualLearningClosure,
    EvaluatorReflector,
    ReplayMemorySource,
    WorldStateVerifier,
    default_action_mapper,
)
from agent_engine.reasoners.scripted import ScriptedReasoner

from .helpers import FakePerceptor, make_world


class TestWorldStateVerifier(unittest.TestCase):
    def setUp(self):
        self.v = WorldStateVerifier()

    def test_allows_safe_base_velocity(self):
        ok, _, reason = self.v.verify("drive", {"vx": 0.1}, make_world())
        self.assertTrue(ok, reason)

    def test_blocks_overspeed(self):
        ok, _, reason = self.v.verify("drive", {"vx": 0.5}, make_world())
        self.assertFalse(ok)
        self.assertIn("safety", reason)

    def test_passthrough_when_no_actuation(self):
        ok, _, reason = self.v.verify(
            "navigate_to", {"location": "kitchen"}, make_world()
        )
        self.assertTrue(ok)
        self.assertIn("passthrough", reason)

    def test_action_mapper_shapes(self):
        self.assertEqual(default_action_mapper("drive", {"vx": 0.1}).shape, (3,))
        self.assertEqual(
            default_action_mapper("arm", {"joint_deltas": [0.01] * 6}).shape, (9,)
        )
        self.assertIsNone(default_action_mapper("navigate_to", {"location": "x"}))


class TestEvaluatorReflector(unittest.TestCase):
    def test_success_episode(self):
        refl = EvaluatorReflector().reflect(
            Goal("inspect"), make_world(), (ToolResult(True, "done"),)
        )
        self.assertTrue(refl.success)
        self.assertGreaterEqual(refl.confidence, 0.8)

    def test_failure_episode(self):
        refl = EvaluatorReflector().reflect(
            Goal("inspect"), make_world(), (ToolResult(False, "nav failed"),)
        )
        self.assertFalse(refl.success)

    def test_persists_labelled_episode(self):
        from learning_engine.data.replay_dataset import ReplayDataset

        with tempfile.TemporaryDirectory() as tmp:
            ds = ReplayDataset(f"{tmp}/ds", store_images=False)
            EvaluatorReflector(dataset=ds, environment="sim").reflect(
                Goal("pick up the cup"), make_world(), (ToolResult(True, "ok"),)
            )
            self.assertEqual(len(ds), 1)
            stats = ds.stats()
            self.assertEqual(stats["episodes"], 1)
            # Episode was labelled by the evaluator (not left UNKNOWN).
            self.assertNotIn("outcome/unknown", stats)

    def test_learns_object_locations_on_success(self):
        from agent_engine.core.blackboard import DetectedObject

        world = make_world(
            objects=[DetectedObject("red_cup", distance_m=0.4)],
            extra={"location_name": "kitchen"},
        )
        refl = EvaluatorReflector().reflect(
            Goal("find red_cup"), world, (ToolResult(True, "ok"),)
        )
        self.assertEqual(refl.learned_objects.get("red_cup"), "kitchen")


class TestReplayMemorySource(unittest.TestCase):
    def test_summaries_from_dataset(self):
        from learning_engine.data.replay_dataset import ReplayDataset

        with tempfile.TemporaryDirectory() as tmp:
            ds = ReplayDataset(f"{tmp}/ds", store_images=False)
            EvaluatorReflector(dataset=ds).reflect(
                Goal("scan the room"), make_world(), (ToolResult(True, "ok"),)
            )
            text = ReplayMemorySource(ds)()
            self.assertIn("Past episodes", text)
            self.assertIn("scan the room", text)


class _StubScheduler:
    def __init__(self):
        self.steps = 0

    def pending_triggers(self):
        return ["new_episodes:1"]

    def step(self, force=False):
        self.steps += 1
        return {"ran": True, "force": force}


class TestContinualLearningClosure(unittest.TestCase):
    def test_delegates_to_scheduler(self):
        closure = ContinualLearningClosure(_StubScheduler())
        self.assertEqual(closure.pending(), ["new_episodes:1"])
        self.assertEqual(closure.maybe_learn(force=True), {"ran": True, "force": True})


class TestHarnessWithRealVerifier(unittest.TestCase):
    def test_real_safety_gate_blocks_overspeed_low_level_tool(self):
        log = []
        reg = ToolRegistry()
        reg.register(
            ToolSpec(
                "drive",
                "low-level base velocity",
                lambda vx: log.append(f"drive:{vx}") or ToolResult(True, "drove"),
                [ToolParam("vx", "linear x", type="number")],
                low_level=True,
            )
        )
        plans = [Plan(calls=[ToolCall("drive", {"vx": 0.9})]), Plan(goal_complete=True)]
        harness = AgentHarness(
            FakePerceptor(make_world()),
            ScriptedReasoner(plans),
            reg,
            reflector=EvaluatorReflector(),
            verifier=WorldStateVerifier(),
        )
        harness.submit_goal("lurch forward")
        harness.run_until_idle()
        self.assertEqual(
            log, []
        )  # overspeed never actuated — real SafetyCheck gated it


if __name__ == "__main__":
    unittest.main()
