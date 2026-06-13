"""Data collection layer — every way experience enters the system.

| Collector                | Source                                          |
|--------------------------|-------------------------------------------------|
| SimRolloutCollector      | policy rollouts in any SimulationEnv            |
| TeleopDatasetCollector   | LeRobot datasets from teleop_recorder_node      |
| RosbagCollector          | ros2 bags via data_engine/ingestion             |
| ExecutionLogCollector    | jsonl/npz logs from ros2/episode_logger_node    |

Human demonstrations arrive through either the teleop path (leader arm) or
rosbags — there is intentionally no separate format for them; they are
Episodes with ``source=DataSource.HUMAN_DEMO``.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any, Iterable, Iterator, Optional

import numpy as np

from ..core.interfaces import DataCollector, Policy, SimulationEnv
from ..core.registry import COLLECTORS
from ..core.types import DataSource, Episode, EpisodeMeta, Step
from . import schema


@COLLECTORS.register("sim_rollout")
class SimRolloutCollector(DataCollector):
    """Roll a policy in a simulation env and record episodes.

    The env's reward/done/info are stored in ``Step.info`` (keys
    ``env_reward``, ``done``, plus the env's info dict) so the reward engine
    can re-annotate offline without losing the sim's native signal.
    """

    def __init__(
        self,
        env: SimulationEnv,
        policy: Policy,
        max_steps: int = 200,
        task_instruction: str = "",
        environment_name: str = "simulation",
    ) -> None:
        self.env = env
        self.policy = policy
        self.max_steps = max_steps
        self.task_instruction = task_instruction
        self.environment_name = environment_name

    def collect(self, num_episodes: int = 1, **kwargs: Any) -> Iterator[Episode]:
        for _ in range(num_episodes):
            obs = self.env.reset(**kwargs)
            steps = []
            for t in range(self.max_steps):
                action = self.policy.predict(obs, task=self.task_instruction)
                next_obs, env_reward, done, info = self.env.step(action)
                steps.append(
                    Step(
                        observation=obs,
                        action=np.asarray(action, dtype=np.float32),
                        timestamp=float(t),
                        info={
                            "env_reward": float(env_reward),
                            "done": bool(done),
                            **info,
                        },
                    )
                )
                obs = next_obs
                if done:
                    break
            yield Episode(
                meta=EpisodeMeta(
                    task_instruction=self.task_instruction,
                    source=DataSource.SIMULATION,
                    environment=self.environment_name,
                ),
                steps=steps,
            )


@COLLECTORS.register("execution_log")
class ExecutionLogCollector(DataCollector):
    """Ingest episodes written by ``learning_engine/ros2/episode_logger_node``.

    The logger node writes one directory per episode containing ``meta.json``
    and ``steps.npz`` in the same layout as ReplayDataset; this collector
    simply loads any directory not yet marked ingested.
    """

    def __init__(self, log_dir: str) -> None:
        self.log_dir = Path(log_dir).expanduser()

    def collect(self, **kwargs: Any) -> Iterator[Episode]:
        from .replay_dataset import ReplayDataset

        if not self.log_dir.exists():
            return
        staging = ReplayDataset(str(self.log_dir))
        for eid in staging.episode_ids():
            marker = staging.episodes_dir / eid / ".ingested"
            if marker.exists():
                continue
            episode = staging.get(eid)
            episode.meta.source = DataSource.REAL_EXECUTION
            marker.touch()
            yield episode


@COLLECTORS.register("teleop_dataset")
class TeleopDatasetCollector(DataCollector):
    """Convert LeRobot datasets recorded by ``teleop_recorder_node`` /
    ``lerobot_engine/record.py`` into Episodes (source=TELEOP).

    Requires ``lerobot``; on the Pi or in CI without it, this collector
    reports clearly instead of failing at import time.
    """

    def __init__(
        self,
        repo_id: str,
        root: Optional[str] = None,
        source: DataSource = DataSource.TELEOP,
    ) -> None:
        self.repo_id = repo_id
        self.root = root
        self.source = source

    def collect(self, **kwargs: Any) -> Iterator[Episode]:
        try:
            from lerobot.common.datasets.lerobot_dataset import LeRobotDataset
        except ImportError as e:
            raise RuntimeError(
                "TeleopDatasetCollector requires `lerobot` "
                "(pip install lerobot) — run it on the training workstation"
            ) from e

        ds = LeRobotDataset(self.repo_id, root=self.root)
        inv_map = {v: k for k, v in schema.LEROBOT_KEY_MAP.items()}
        for ep_idx in range(ds.num_episodes):
            frame_ids = range(
                ds.episode_data_index["from"][ep_idx].item(),
                ds.episode_data_index["to"][ep_idx].item(),
            )
            steps = []
            task = ""
            for i in frame_ids:
                frame = ds[i]
                task = frame.get("task", task)
                obs = {}
                for lerobot_key, ours in inv_map.items():
                    if lerobot_key in frame:
                        arr = frame[lerobot_key]
                        obs[ours] = (
                            arr.numpy() if hasattr(arr, "numpy") else np.asarray(arr)
                        )
                steps.append(
                    Step(
                        observation=obs,
                        action=np.asarray(frame[schema.LEROBOT_ACTION_KEY]),
                        timestamp=float(frame.get("timestamp", 0.0)),
                    )
                )
            yield Episode(
                meta=EpisodeMeta(
                    task_instruction=str(task),
                    source=self.source,
                    environment="real",
                    extra={"lerobot_repo_id": self.repo_id, "lerobot_episode": ep_idx},
                ),
                steps=steps,
            )


@COLLECTORS.register("rosbag")
class RosbagCollector(DataCollector):
    """Ingest ros2 bags through the existing ``data_engine`` pipeline
    (``data_engine/ingestion/bag_to_omnibot.py`` handles topic sync at
    0.05 s tolerance). This collector wraps its output directory."""

    def __init__(self, bag_path: str, work_dir: str = "~/.omnibot/bag_ingest") -> None:
        self.bag_path = Path(bag_path).expanduser()
        self.work_dir = Path(work_dir).expanduser()

    def collect(self, **kwargs: Any) -> Iterable[Episode]:
        raise NotImplementedError(
            "Run `python data_engine/ingestion/bag_to_omnibot.py "
            f"--bag {self.bag_path} --output {self.work_dir}` first, then "
            "ingest with ExecutionLogCollector(work_dir). Direct in-process "
            "ingestion needs rosbag2_py, which is only present on ROS machines."
        )
