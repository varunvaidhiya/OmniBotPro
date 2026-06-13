"""Types, registry, and ReplayDataset round-trip."""

from __future__ import annotations

import tempfile
import unittest

import numpy as np

from learning_engine.core.registry import Registry
from learning_engine.core.types import RewardBreakdown, TaskOutcome
from learning_engine.data import schema
from learning_engine.data.replay_dataset import ReplayDataset
from learning_engine.rewards.engine import RewardEngine

from .helpers import make_episode


class TestTypes(unittest.TestCase):
    def test_reward_breakdown_total(self):
        b = RewardBreakdown(terms={"a": 1.0, "b": -2.0}, weights={"a": 2.0})
        self.assertAlmostEqual(b.total, 2.0 * 1.0 + 1.0 * -2.0)

    def test_episode_transitions(self):
        ep = make_episode(3)
        transitions = list(ep.transitions())
        self.assertEqual(len(transitions), 3)
        self.assertTrue(transitions[-1].done)
        self.assertFalse(transitions[0].done)
        np.testing.assert_array_equal(
            transitions[0].next_observation[schema.OBS_STATE],
            ep.steps[1].observation[schema.OBS_STATE],
        )


class TestRegistry(unittest.TestCase):
    def test_register_create_and_errors(self):
        reg = Registry("thing")

        @reg.register("foo")
        class Foo:
            def __init__(self, x=1):
                self.x = x

        self.assertEqual(reg.create("foo", x=5).x, 5)
        self.assertEqual(reg.names(), ["foo"])
        with self.assertRaises(KeyError):
            reg.get("bar")
        with self.assertRaises(KeyError):
            reg.register("foo")(Foo)


class TestReplayDataset(unittest.TestCase):
    def test_roundtrip_and_filters(self):
        ep = make_episode(4)
        engine = RewardEngine.from_config(
            [{"name": "task_success"}, {"name": "energy", "weight": 0.1}]
        )
        engine.annotate_episode(ep)

        with tempfile.TemporaryDirectory() as root:
            ds = ReplayDataset(root)
            eid = ds.add_episode(ep)
            self.assertEqual(len(ds), 1)

            loaded = ds.get(eid)
            self.assertEqual(len(loaded), 4)
            self.assertEqual(loaded.meta.task_instruction, ep.meta.task_instruction)
            np.testing.assert_allclose(loaded.steps[2].action, ep.steps[2].action)
            np.testing.assert_array_equal(
                loaded.steps[1].observation[schema.OBS_IMAGE_WRIST],
                ep.steps[1].observation[schema.OBS_IMAGE_WRIST],
            )
            self.assertAlmostEqual(
                loaded.steps[-1].reward.total, ep.steps[-1].reward.total, places=5
            )

            # Relabel after evaluation, then filter.
            ds.update_meta(eid, outcome=TaskOutcome.SUCCESS, success_score=0.9)
            self.assertEqual(ds.episode_ids(outcome=TaskOutcome.SUCCESS), [eid])
            self.assertEqual(ds.episode_ids(outcome=TaskOutcome.FAILURE), [])

            # Reopen from disk — index must survive.
            ds2 = ReplayDataset(root)
            self.assertEqual(len(ds2), 1)
            self.assertEqual(ds2.stats()["outcome/success"], 1)

    def test_store_images_false(self):
        with tempfile.TemporaryDirectory() as root:
            ds = ReplayDataset(root, store_images=False)
            eid = ds.add_episode(make_episode(2))
            loaded = ds.get(eid)
            self.assertIn(schema.OBS_STATE, loaded.steps[0].observation)
            self.assertNotIn(schema.OBS_IMAGE_WRIST, loaded.steps[0].observation)


if __name__ == "__main__":
    unittest.main()
