"""Tests for the blackboard and the tool registry."""

from __future__ import annotations

import unittest

import numpy as np

from agent_engine.core.blackboard import DetectedObject, WorldState
from agent_engine.core.tools import ToolParam, ToolRegistry, ToolSpec
from agent_engine.core.types import ToolCall, ToolResult


class TestWorldState(unittest.TestCase):
    def test_to_observation_is_9d_state(self):
        ws = WorldState(
            arm_joint_positions=[0.1, 0.2, 0.3, 0.4, 0.5, 0.6],
            base_velocity=(0.1, 0.0, 0.2),
        )
        obs = ws.to_observation()
        self.assertIn("state", obs)
        self.assertEqual(obs["state"].shape, (9,))
        np.testing.assert_allclose(obs["state"][6:], [0.1, 0.0, 0.2], atol=1e-6)

    def test_to_observation_pads_missing_arm_joints(self):
        ws = WorldState(arm_joint_positions=[0.1, 0.2])  # only 2 reported
        self.assertEqual(ws.to_observation()["state"].shape, (9,))

    def test_nearest_and_by_label(self):
        ws = WorldState(
            detected_objects=[
                DetectedObject("cup", distance_m=0.8),
                DetectedObject("bottle", distance_m=0.4),
                DetectedObject("cup", distance_m=0.3),
            ]
        )
        self.assertEqual(ws.nearest_object().label, "cup")
        self.assertAlmostEqual(ws.nearest_object().distance_m, 0.3)
        self.assertAlmostEqual(ws.object_by_label("CUP").distance_m, 0.3)
        self.assertIsNone(ws.object_by_label("banana"))

    def test_dict_round_trip(self):
        ws = WorldState(
            base_pose=(1.0, 2.0, 0.5),
            arm_joint_positions=[0.1] * 6,
            detected_objects=[DetectedObject("cup", confidence=0.9, distance_m=0.5)],
            mission_phase="navigating",
            scene_description="a cup on a table",
        )
        back = WorldState.from_dict(ws.to_dict())
        self.assertEqual(back.base_pose, (1.0, 2.0, 0.5))
        self.assertEqual(back.mission_phase, "navigating")
        self.assertEqual(back.detected_objects[0].label, "cup")
        self.assertAlmostEqual(back.detected_objects[0].confidence, 0.9)

    def test_summarize_mentions_objects_and_estop(self):
        ws = WorldState(
            detected_objects=[DetectedObject("cup", distance_m=0.5)],
            emergency_stop=True,
        )
        text = ws.summarize()
        self.assertIn("cup", text)
        self.assertIn("EMERGENCY STOP", text)


class TestToolRegistry(unittest.TestCase):
    def setUp(self):
        self.log = []
        self.reg = ToolRegistry()
        self.reg.register(
            ToolSpec(
                "navigate_to",
                "go somewhere",
                lambda location: (
                    self.log.append(location) or ToolResult(True, f"to {location}")
                ),
                [ToolParam("location", "where")],
            )
        )

    def test_dispatch_ok(self):
        res = self.reg.dispatch(ToolCall("navigate_to", {"location": "kitchen"}))
        self.assertTrue(res.ok)
        self.assertEqual(self.log, ["kitchen"])

    def test_unknown_tool(self):
        res = self.reg.dispatch(ToolCall("teleport", {}))
        self.assertFalse(res.ok)
        self.assertIn("unknown tool", res.output)

    def test_missing_required_arg(self):
        res = self.reg.dispatch(ToolCall("navigate_to", {}))
        self.assertFalse(res.ok)
        self.assertIn("missing required", res.output)

    def test_handler_exception_becomes_result(self):
        self.reg.register(
            ToolSpec("boom", "raises", lambda: (_ for _ in ()).throw(RuntimeError("x")))
        )
        res = self.reg.dispatch(ToolCall("boom", {}))
        self.assertFalse(res.ok)
        self.assertIn("raised", res.output)

    def test_duplicate_registration_rejected(self):
        with self.assertRaises(ValueError):
            self.reg.register(ToolSpec("navigate_to", "dup", lambda: None))

    def test_anthropic_schema_shape(self):
        schema = self.reg.to_anthropic_schema()
        self.assertEqual(schema[0]["name"], "navigate_to")
        self.assertIn("location", schema[0]["input_schema"]["properties"])
        self.assertIn("location", schema[0]["input_schema"]["required"])

    def test_decorator_form(self):
        reg = ToolRegistry()

        @reg.tool("wait", "wait a bit", [ToolParam("seconds", "n", type="number")])
        def _wait(seconds):  # noqa: ANN001
            return ToolResult(True, f"waited {seconds}")

        self.assertIn("wait", reg)
        self.assertTrue(reg.dispatch(ToolCall("wait", {"seconds": 1})).ok)


if __name__ == "__main__":
    unittest.main()
