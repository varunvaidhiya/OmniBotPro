"""Continual learning — when and on what to retrain, plus policy versioning.

``ContinualLearningScheduler`` watches the ReplayDataset and fires the
post-training loop when any trigger is met:

- ``min_new_episodes``  — enough new experience accumulated
- ``new_task``          — an instruction never seen at last training time
- ``max_staleness_s``   — periodic refresh regardless of volume

``PolicyVersionManager`` records every produced checkpoint with the dataset
snapshot it was trained on, enabling rollback when an evaluation regresses.
Catastrophic-forgetting mitigation is data-side by design: retraining always
mixes old episodes with new ones (the EpisodicReplayStore's stratified
sampling), rather than relying on algorithmic regularization.
"""

from __future__ import annotations

import json
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Set

from ..data.replay_dataset import ReplayDataset
from .post_training_loop import IterationReport, PostTrainingLoop


@dataclass
class ContinualState:
    last_train_time: float = 0.0
    episodes_at_last_train: int = 0
    known_tasks: Set[str] = field(default_factory=set)
    known_environments: Set[str] = field(default_factory=set)


class ContinualLearningScheduler:
    def __init__(
        self,
        loop: PostTrainingLoop,
        dataset: ReplayDataset,
        min_new_episodes: int = 50,
        max_staleness_s: float = 7 * 24 * 3600.0,
        retrain_on_new_task: bool = True,
        state_path: str = "",
    ) -> None:
        self.loop = loop
        self.dataset = dataset
        self.min_new_episodes = min_new_episodes
        self.max_staleness_s = max_staleness_s
        self.retrain_on_new_task = retrain_on_new_task
        self.state_path = Path(state_path).expanduser() if state_path else None
        self.state = self._load_state()

    # -------------------------------------------------------------- trigger
    def pending_triggers(self) -> List[str]:
        triggers = []
        new_eps = len(self.dataset) - self.state.episodes_at_last_train
        if new_eps >= self.min_new_episodes:
            triggers.append(f"new_episodes:{new_eps}")
        tasks, envs = self._current_tasks_envs()
        new_tasks = tasks - self.state.known_tasks
        if self.retrain_on_new_task and new_tasks:
            triggers.append(f"new_tasks:{sorted(new_tasks)[:3]}")
        new_envs = envs - self.state.known_environments
        if new_envs:
            triggers.append(f"new_environments:{sorted(new_envs)[:3]}")
        age = time.time() - self.state.last_train_time
        if self.state.last_train_time and age > self.max_staleness_s:
            triggers.append(f"staleness:{age / 3600:.0f}h")
        return triggers

    def step(self, force: bool = False) -> Optional[IterationReport]:
        """Check triggers and run one loop iteration when warranted."""
        triggers = self.pending_triggers()
        if not (triggers or force):
            return None
        report = self.loop.run_iteration()
        tasks, envs = self._current_tasks_envs()
        self.state = ContinualState(
            last_train_time=time.time(),
            episodes_at_last_train=len(self.dataset),
            known_tasks=tasks,
            known_environments=envs,
        )
        self._save_state()
        return report

    # -------------------------------------------------------------- private
    def _current_tasks_envs(self):
        index = self.dataset._index  # scheduler is a friend of the dataset
        tasks = {e["task_instruction"] for e in index.values() if e["task_instruction"]}
        envs = {e["environment"] for e in index.values() if e["environment"]}
        return tasks, envs

    def _load_state(self) -> ContinualState:
        if self.state_path and self.state_path.exists():
            doc = json.loads(self.state_path.read_text())
            return ContinualState(
                last_train_time=doc["last_train_time"],
                episodes_at_last_train=doc["episodes_at_last_train"],
                known_tasks=set(doc["known_tasks"]),
                known_environments=set(doc["known_environments"]),
            )
        return ContinualState()

    def _save_state(self) -> None:
        if not self.state_path:
            return
        self.state_path.parent.mkdir(parents=True, exist_ok=True)
        self.state_path.write_text(
            json.dumps(
                {
                    "last_train_time": self.state.last_train_time,
                    "episodes_at_last_train": self.state.episodes_at_last_train,
                    "known_tasks": sorted(self.state.known_tasks),
                    "known_environments": sorted(self.state.known_environments),
                },
                indent=1,
            )
        )


class PolicyVersionManager:
    """Append-only registry of trained checkpoints with eval results, so a
    regression found after deployment can be rolled back to any prior
    version (versions.jsonl lives next to the checkpoints)."""

    def __init__(self, registry_dir: str) -> None:
        self.dir = Path(registry_dir).expanduser()
        self.dir.mkdir(parents=True, exist_ok=True)
        self.path = self.dir / "versions.jsonl"

    def record(
        self,
        checkpoint_path: str,
        trainer: str,
        eval_success_rate: float,
        dataset_episodes: int,
        notes: str = "",
    ) -> Dict:
        entry = {
            "version": len(self.all()) + 1,
            "checkpoint_path": checkpoint_path,
            "trainer": trainer,
            "eval_success_rate": eval_success_rate,
            "dataset_episodes": dataset_episodes,
            "created_at": time.time(),
            "notes": notes,
        }
        with self.path.open("a") as f:
            f.write(json.dumps(entry) + "\n")
        return entry

    def all(self) -> List[Dict]:
        if not self.path.exists():
            return []
        return [json.loads(line) for line in self.path.read_text().splitlines() if line]

    def best(self) -> Optional[Dict]:
        versions = [
            v for v in self.all() if v["eval_success_rate"] == v["eval_success_rate"]
        ]
        return max(versions, key=lambda v: v["eval_success_rate"]) if versions else None

    def latest(self) -> Optional[Dict]:
        versions = self.all()
        return versions[-1] if versions else None
