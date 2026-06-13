"""Replay buffers, inference verification, curriculum, and the full loop."""

from __future__ import annotations

import tempfile
import unittest

import numpy as np

from learning_engine.core.types import TaskOutcome
from learning_engine.data import schema
from learning_engine.data.collectors import SimRolloutCollector
from learning_engine.data.replay_dataset import ReplayDataset
from learning_engine.evaluation.self_eval import HeuristicSelfEvaluator
from learning_engine.loop.continual import (
    ContinualLearningScheduler,
    PolicyVersionManager,
)
from learning_engine.loop.post_training_loop import LoopComponents, PostTrainingLoop
from learning_engine.policies.base import RandomPolicy, ZeroPolicy
from learning_engine.policies.trainers import NoOpTrainer
from learning_engine.replay.buffer import (
    EpisodicReplayStore,
    PrioritizedReplayBuffer,
    UniformReplayBuffer,
)
from learning_engine.rewards.engine import RewardEngine
from learning_engine.sim.curriculum import CurriculumScheduler, CurriculumStage
from learning_engine.verification.verifier import (
    InferenceVerifier,
    ReachabilityCheck,
    SafetyCheck,
)

from .helpers import DummyEnv, make_episode


class TestBuffers(unittest.TestCase):
    def test_uniform_ring(self):
        buf = UniformReplayBuffer(capacity=10, seed=0)
        for _ in range(3):
            buf.add_episode(make_episode(5))
        self.assertEqual(len(buf), 10)  # capped at capacity
        self.assertEqual(len(buf.sample(4)), 4)

    def test_prioritized_sampling_and_weights(self):
        buf = PrioritizedReplayBuffer(capacity=100, seed=0)
        buf.add_episode(make_episode(10), priority=0.01)
        buf.add_episode(make_episode(10), priority=10.0)
        transitions, idx, weights = buf.sample_with_weights(32)
        self.assertEqual(len(transitions), 32)
        self.assertEqual(weights.max(), 1.0)
        # High-priority half (indices 10..19) should dominate samples.
        self.assertGreater(np.mean(idx >= 10), 0.7)
        buf.update_priorities(idx, np.zeros(len(idx)))
        self.assertEqual(len(buf), 20)

    def test_episodic_store_stratified(self):
        with tempfile.TemporaryDirectory() as root:
            ds = ReplayDataset(root, store_images=False)
            for outcome in (
                TaskOutcome.SUCCESS,
                TaskOutcome.SUCCESS,
                TaskOutcome.FAILURE,
                TaskOutcome.NEAR_SUCCESS,
            ):
                ep = make_episode(3)
                ep.meta.outcome = outcome
                ds.add_episode(ep)
            store = EpisodicReplayStore(ds, seed=0)
            ids = store.sample_episode_ids(3)
            self.assertTrue(ids)
            buf = UniformReplayBuffer()
            n = store.fill_transition_buffer(buf, 3)
            self.assertEqual(len(buf), n)
            self.assertGreater(n, 0)


class TestVerifier(unittest.TestCase):
    def _verifier(self):
        return InferenceVerifier(
            checks=[SafetyCheck(), ReachabilityCheck()],
            n_candidates=4,
            horizon=5,
            fallback_policy=ZeroPolicy(action_dim=9),
        )

    def _obs(self):
        return {
            schema.OBS_STATE: np.zeros(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32)
        }

    def test_safe_policy_selected(self):
        result = self._verifier().select(
            RandomPolicy(action_dim=9, scale=0.01, seed=0), self._obs()
        )
        self.assertFalse(result.fallback_used)
        self.assertTrue(result.plan.feasible)
        self.assertIn("consistency", result.plan.scores)
        self.assertEqual(result.action.shape, (9,))

    def test_unsafe_policy_falls_back(self):
        class Unsafe(RandomPolicy):
            def predict(self, observation, task=""):
                a = np.zeros(9, dtype=np.float32)
                a[schema.ARM_DIM] = 0.9  # 0.9 m/s >> 0.2 m/s clamp
                return a

        result = self._verifier().select(Unsafe(action_dim=9, seed=0), self._obs())
        self.assertTrue(result.fallback_used)
        np.testing.assert_array_equal(result.action, np.zeros(9, dtype=np.float32))
        self.assertTrue(all(not c.feasible for c in result.candidates))
        self.assertIn("safety", result.candidates[0].rejection_reason)

    def test_arm_delta_rejected(self):
        verifier = self._verifier()
        from learning_engine.core.types import CandidatePlan

        actions = np.zeros((5, 9), dtype=np.float32)
        actions[2, 0] = 1.0  # 1 rad jump in one step
        plan = CandidatePlan(actions=actions)
        score, reason = SafetyCheck().check(plan, self._obs(), {})
        self.assertEqual(score, 0.0)
        self.assertIn("arm joint delta", reason)
        del verifier


class TestCurriculum(unittest.TestCase):
    def test_promotion(self):
        sched = CurriculumScheduler(
            [
                CurriculumStage(
                    "easy", {"r": 1.0}, window=4, promote_success_rate=0.75
                ),
                CurriculumStage("hard", {"r": 3.0}, window=4),
            ]
        )
        self.assertEqual(sched.current.name, "easy")
        promoted = [sched.report(True) for _ in range(4)]
        self.assertTrue(promoted[-1])
        self.assertEqual(sched.current.name, "hard")


class TestPostTrainingLoop(unittest.TestCase):
    def _components(self, root: str) -> LoopComponents:
        env = DummyEnv()
        policy = RandomPolicy(
            action_dim=schema.MOBILE_MANIP_ACTION_DIM, scale=0.05, seed=1
        )
        dataset = ReplayDataset(root, store_images=False)
        return LoopComponents(
            collectors=[
                SimRolloutCollector(
                    env,
                    policy,
                    max_steps=10,
                    task_instruction="reach the goal",
                    environment_name="dummy",
                )
            ],
            dataset=dataset,
            reward_engine=RewardEngine.from_config(
                [
                    {"name": "task_success"},
                    {"name": "time"},
                    {"name": "action_smoothness", "weight": 0.1},
                ]
            ),
            trainer=NoOpTrainer(),
            policy=policy,
            evaluators=[HeuristicSelfEvaluator()],
            replay_store=EpisodicReplayStore(dataset, seed=0),
            eval_collector=SimRolloutCollector(
                DummyEnv(), policy, max_steps=10, task_instruction="reach the goal"
            ),
        )

    def test_full_iteration(self):
        with tempfile.TemporaryDirectory() as root:
            loop = PostTrainingLoop(
                self._components(root),
                collect_per_iteration=3,
                eval_episodes=2,
                report_dir=f"{root}/reports",
            )
            report = loop.run_iteration()
            self.assertEqual(report.episodes_collected, 3)
            self.assertEqual(report.outcomes.get("success"), 3)  # DummyEnv succeeds
            self.assertEqual(report.train_result.trainer, "noop")
            self.assertGreater(report.train_result.metrics["transitions"], 0)
            self.assertEqual(report.eval_success_rate, 1.0)
            # Episodes were persisted with evaluation metadata.
            ds = ReplayDataset(root, store_images=False)
            self.assertEqual(len(ds), 3)
            ep = ds.get(ds.episode_ids()[0])
            self.assertIs(ep.meta.outcome, TaskOutcome.SUCCESS)
            self.assertIn("evaluation", ep.meta.extra)
            # Iteration report written to disk.
            import os

            self.assertTrue(os.path.exists(f"{root}/reports/iteration_00001.json"))

    def test_continual_scheduler_and_versioning(self):
        with tempfile.TemporaryDirectory() as root:
            comps = self._components(f"{root}/ds")
            loop = PostTrainingLoop(comps, collect_per_iteration=2, eval_episodes=1)
            sched = ContinualLearningScheduler(
                loop,
                comps.dataset,
                min_new_episodes=1,
                state_path=f"{root}/state.json",
            )
            # Empty dataset, no triggers yet -> force the bootstrap run.
            self.assertIsNone(sched.step(force=False))
            report = sched.step(force=True)
            self.assertIsNotNone(report)
            # No new data since the run -> no trigger.
            self.assertIsNone(sched.step())
            # New experience arriving (e.g. fresh demos) re-triggers training.
            comps.dataset.add_episode(make_episode(3, task="open the drawer"))
            self.assertEqual(
                sorted(t.split(":")[0] for t in sched.pending_triggers()),
                ["new_episodes", "new_tasks"],
            )
            report2 = sched.step()
            self.assertIsNotNone(report2)
            self.assertTrue(sched.state.known_tasks)

            versions = PolicyVersionManager(f"{root}/versions")
            versions.record("ckpt1.pt", "noop", 0.5, 2)
            versions.record("ckpt2.pt", "noop", 0.9, 4)
            self.assertEqual(versions.best()["checkpoint_path"], "ckpt2.pt")
            self.assertEqual(versions.latest()["version"], 2)


if __name__ == "__main__":
    unittest.main()
