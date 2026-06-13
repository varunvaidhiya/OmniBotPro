"""Reward engine, terms, vision reward parsing, and evaluators."""

from __future__ import annotations

import unittest

import numpy as np

from learning_engine.core.types import Transition
from learning_engine.data import schema
from learning_engine.evaluation.judge import StaticVLMClient
from learning_engine.evaluation.language_goal import LanguageGoalEvaluator
from learning_engine.evaluation.self_eval import HeuristicSelfEvaluator
from learning_engine.core.types import TaskOutcome
from learning_engine.rewards.engine import RewardEngine
from learning_engine.rewards.vision import VLMRewardModel, sample_frames

from .helpers import make_episode


def _transition(action=None, info=None, state=None):
    state = (
        state
        if state is not None
        else np.zeros(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32)
    )
    obs = {schema.OBS_STATE: state}
    return Transition(
        observation=obs,
        action=np.asarray(
            action if action is not None else np.zeros(9), dtype=np.float32
        ),
        reward=0.0,
        next_observation=obs,
        done=False,
        info=info or {},
    )


class TestRewardEngine(unittest.TestCase):
    def test_multi_objective_breakdown(self):
        engine = RewardEngine.from_config(
            [
                {"name": "task_success", "weight": 1.0, "bonus": 10.0},
                {"name": "energy", "weight": 0.5},
                {"name": "time", "per_step": 0.01},
            ]
        )
        t = _transition(action=np.ones(9), info={"success": True})
        b = engine.compute(t)
        self.assertEqual(b.terms["task_success"], 10.0)
        self.assertAlmostEqual(b.terms["energy"], -9.0)
        self.assertAlmostEqual(b.total, 10.0 + 0.5 * -9.0 - 0.01)
        self.assertAlmostEqual(t.reward, b.total)

    def test_stateful_terms_reset(self):
        engine = RewardEngine.from_config([{"name": "action_smoothness"}])
        t1 = _transition(action=np.zeros(9))
        t2 = _transition(action=np.ones(9))
        engine.reset()
        self.assertEqual(engine.compute(t1).terms["action_smoothness"], 0.0)
        self.assertAlmostEqual(engine.compute(t2).terms["action_smoothness"], -9.0)
        engine.reset()  # new episode — no penalty against the old action
        self.assertEqual(engine.compute(t2).terms["action_smoothness"], 0.0)

    def test_collision_and_joint_limit(self):
        engine = RewardEngine.from_config(
            [
                {"name": "collision", "weight": 5.0},
                {"name": "joint_limit"},
            ]
        )
        bad_state = np.zeros(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32)
        bad_state[0] = 3.14  # shoulder_pan at its limit
        t = _transition(info={"collision": True}, state=bad_state)
        b = engine.compute(t)
        self.assertEqual(b.terms["collision"], -1.0)
        self.assertLess(b.terms["joint_limit"], 0.0)

    def test_goal_progress(self):
        engine = RewardEngine.from_config([{"name": "goal_progress"}])
        engine.reset()
        ctx1 = {"goal_xy": [1.0, 0.0], "robot_xy": [0.0, 0.0]}
        ctx2 = {"goal_xy": [1.0, 0.0], "robot_xy": [0.5, 0.0]}
        engine.compute(_transition(), ctx1)  # first step: no previous distance
        b = engine.compute(_transition(), ctx2)
        self.assertAlmostEqual(b.terms["goal_progress"], 0.5)


class TestVisionReward(unittest.TestCase):
    def test_sample_frames(self):
        frames = [np.full((2, 2, 3), i, dtype=np.uint8) for i in range(10)]
        picked = sample_frames(frames, 4)
        self.assertEqual(len(picked), 4)
        self.assertEqual(int(picked[0][0, 0, 0]), 0)
        self.assertEqual(int(picked[-1][0, 0, 0]), 9)

    def test_vlm_reward_score_parsing(self):
        client = StaticVLMClient(reply="I'd rate this 0.85 overall.")
        model = VLMRewardModel(client)
        score = model.score("pick up the cup", [np.zeros((2, 2, 3), dtype=np.uint8)])
        self.assertAlmostEqual(score, 0.85)
        self.assertEqual(client.calls[0]["n_images"], 1)


class TestEvaluators(unittest.TestCase):
    def test_heuristic_success(self):
        report = HeuristicSelfEvaluator().evaluate(make_episode(5, success=True))
        self.assertIs(report.outcome, TaskOutcome.SUCCESS)
        self.assertEqual(report.completion_confidence, 1.0)
        self.assertIn("stats", report.extra)

    def test_heuristic_failure(self):
        report = HeuristicSelfEvaluator().evaluate(make_episode(5, success=False))
        self.assertIn(report.outcome, (TaskOutcome.FAILURE, TaskOutcome.NEAR_SUCCESS))

    def test_language_goal_judge(self):
        client = StaticVLMClient(
            reply='{"confidence": 0.9, "rationale": "cup is grasped"}'
        )
        report = LanguageGoalEvaluator(client).evaluate(make_episode(5))
        self.assertIs(report.outcome, TaskOutcome.SUCCESS)
        self.assertAlmostEqual(report.completion_confidence, 0.9)
        self.assertEqual(report.task_summary, "cup is grasped")

    def test_language_goal_unparseable_reply(self):
        report = LanguageGoalEvaluator(StaticVLMClient(reply="gibberish")).evaluate(
            make_episode(3)
        )
        self.assertIs(report.outcome, TaskOutcome.FAILURE)
        self.assertEqual(report.completion_confidence, 0.0)


if __name__ == "__main__":
    unittest.main()
