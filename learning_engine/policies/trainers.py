"""Policy learning strategies — interchangeable behind ``PolicyTrainer``.

| Trainer                  | Data it accepts                  | Backend        |
|--------------------------|----------------------------------|----------------|
| BehaviorCloningTrainer   | ReplayDataset / list[Episode]    | torch (MLP)    |
| OfflineRLTrainer (AWR)   | ReplayDataset / list[Episode]    | torch (MLP)    |
| OnlineRLTrainer          | Isaac Lab task name              | rl_engine PPO  |
| FineTuneTrainer          | LeRobot dataset repo_id          | lerobot_engine |

The first two are implemented here (small state-based MLPs — useful as
baselines and for distilling RL policies). The last two intentionally
*delegate* to the existing training stacks rather than duplicating them:
OnlineRLTrainer shells into ``rl_engine/scripts/train_*.py`` (Isaac Lab must
own its process) and FineTuneTrainer into ``lerobot_engine/train.py``
(SmolVLA fine-tuning).
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path
from typing import Any, List, Optional, Sequence, Tuple

import numpy as np

from ..core.interfaces import Policy, PolicyTrainer
from ..core.registry import POLICIES, TRAINERS
from ..core.types import Episode, TrainResult
from ..data import schema
from ..data.replay_dataset import ReplayDataset

# ---------------------------------------------------------------------------
# Torch MLP policy + BC / offline-RL trainers
# ---------------------------------------------------------------------------


def _require_torch():
    try:
        import torch
        import torch.nn as nn
    except ImportError as e:
        raise RuntimeError(
            "This trainer requires torch (`pip install torch`) — run it on "
            "the GPU workstation, not the Pi"
        ) from e
    return torch, nn


@POLICIES.register("mlp")
class MlpPolicy(Policy):
    """Small state→action MLP (torch). The trainable policy for the
    in-process BC / offline-RL baselines."""

    def __init__(
        self,
        state_dim: int = schema.MOBILE_MANIP_STATE_DIM,
        action_dim: int = schema.MOBILE_MANIP_ACTION_DIM,
        hidden: int = 256,
        device: str = "auto",
    ) -> None:
        torch, nn = _require_torch()
        from ..hardware import resolve_device

        self._torch = torch
        self.action_dim = action_dim
        self.device = resolve_device(device)
        self.net = nn.Sequential(
            nn.Linear(state_dim, hidden),
            nn.ReLU(),
            nn.Linear(hidden, hidden),
            nn.ReLU(),
            nn.Linear(hidden, action_dim),
            nn.Tanh(),
        ).to(device)

    def predict(self, observation, task: str = "") -> np.ndarray:
        torch = self._torch
        x = torch.as_tensor(
            observation[schema.OBS_STATE], dtype=torch.float32, device=self.device
        ).reshape(1, -1)
        with torch.no_grad():
            return self.net(x).cpu().numpy().reshape(-1)

    def save(self, path: str) -> None:
        self._torch.save(self.net.state_dict(), path)

    def load(self, path: str) -> None:
        self.net.load_state_dict(self._torch.load(path, map_location=self.device))


def _episodes_from(dataset: Any) -> List[Episode]:
    if isinstance(dataset, ReplayDataset):
        return list(dataset.iter_episodes())
    return list(dataset)


def _state_action_arrays(
    episodes: Sequence[Episode], gamma: float = 0.99
) -> Tuple[np.ndarray, np.ndarray, np.ndarray]:
    states, actions, returns = [], [], []
    for ep in episodes:
        rewards = [
            s.reward.total if s.reward else s.info.get("env_reward", 0.0)
            for s in ep.steps
        ]
        # Discounted reward-to-go per step.
        rtg, acc = [], 0.0
        for r in reversed(rewards):
            acc = r + gamma * acc
            rtg.append(acc)
        rtg.reverse()
        for step, g in zip(ep.steps, rtg):
            state = step.observation.get(schema.OBS_STATE)
            if state is None:
                continue
            states.append(np.asarray(state, dtype=np.float32))
            actions.append(np.asarray(step.action, dtype=np.float32))
            returns.append(g)
    if not states:
        raise ValueError("no transitions with a 'state' observation to train on")
    return np.stack(states), np.stack(actions), np.asarray(returns, dtype=np.float32)


class _SupervisedTrainer(PolicyTrainer):
    """Shared minibatch loop for BC and AWR (both are weighted regression)."""

    def __init__(
        self,
        epochs: int = 20,
        batch_size: int = 256,
        lr: float = 1e-3,
        device: str = "auto",
        checkpoint_dir: str = "~/models/learning_engine",
    ) -> None:
        from ..hardware import resolve_device

        self.epochs = epochs
        self.batch_size = batch_size
        self.lr = lr
        self.device = resolve_device(device)
        self.checkpoint_dir = Path(checkpoint_dir).expanduser()

    def _weights(self, returns: np.ndarray) -> np.ndarray:
        raise NotImplementedError

    def train(self, dataset: Any, policy: Optional[Policy] = None) -> TrainResult:
        torch, _ = _require_torch()
        episodes = _episodes_from(dataset)
        states, actions, returns = _state_action_arrays(episodes)
        weights = self._weights(returns)

        if policy is None:
            policy = MlpPolicy(
                state_dim=states.shape[1],
                action_dim=actions.shape[1],
                device=self.device,
            )
        if not isinstance(policy, MlpPolicy):
            raise TypeError(
                f"{self.name} trains MlpPolicy, got {type(policy).__name__}"
            )

        x = torch.as_tensor(states, device=self.device)
        y = torch.as_tensor(actions, device=self.device)
        w = torch.as_tensor(weights, device=self.device).reshape(-1, 1)
        opt = torch.optim.Adam(policy.net.parameters(), lr=self.lr)

        n, last_loss, steps = len(x), float("nan"), 0
        for _ in range(self.epochs):
            perm = torch.randperm(n)
            for i in range(0, n, self.batch_size):
                idx = perm[i : i + self.batch_size]
                pred = policy.net(x[idx])
                loss = (w[idx] * (pred - y[idx]) ** 2).mean()
                opt.zero_grad()
                loss.backward()
                opt.step()
                last_loss, steps = float(loss.item()), steps + 1

        self.checkpoint_dir.mkdir(parents=True, exist_ok=True)
        ckpt = self.checkpoint_dir / f"{self.name}_policy.pt"
        policy.save(str(ckpt))
        return TrainResult(
            trainer=self.name,
            steps=steps,
            final_loss=last_loss,
            metrics={"transitions": float(n), "episodes": float(len(episodes))},
            checkpoint_path=str(ckpt),
        )


@TRAINERS.register("behavior_cloning")
class BehaviorCloningTrainer(_SupervisedTrainer):
    """Supervised regression onto demonstrated actions (uniform weights)."""

    name = "behavior_cloning"

    def _weights(self, returns: np.ndarray) -> np.ndarray:
        return np.ones_like(returns)


@TRAINERS.register("offline_rl_awr")
class OfflineRLTrainer(_SupervisedTrainer):
    """Advantage-Weighted Regression: BC weighted by exp(advantage/β).

    Deliberately the simplest credible offline-RL algorithm — it cannot
    diverge off-distribution like Q-learning variants, and it upgrades
    cleanly to IQL/CQL later behind the same interface."""

    name = "offline_rl_awr"

    def __init__(
        self, beta: float = 1.0, max_weight: float = 20.0, **kwargs: Any
    ) -> None:
        super().__init__(**kwargs)
        self.beta = beta
        self.max_weight = max_weight

    def _weights(self, returns: np.ndarray) -> np.ndarray:
        adv = returns - returns.mean()
        std = returns.std() or 1.0
        return np.clip(np.exp(adv / (self.beta * std)), 0.0, self.max_weight)


# ---------------------------------------------------------------------------
# Delegating trainers — online RL (Isaac Lab) and VLA fine-tuning (LeRobot)
# ---------------------------------------------------------------------------


@TRAINERS.register("online_rl")
class OnlineRLTrainer(PolicyTrainer):
    """Runs ``rl_engine/scripts/train_{nav,arm}.py`` (Isaac Lab PPO) as a
    subprocess and exports ONNX for the omnibot_rl deployment nodes.
    ``dataset`` is ignored — online RL collects its own experience."""

    name = "online_rl"

    def __init__(
        self,
        task: str = "nav",
        num_envs: int = 512,
        max_iterations: int = 2000,
        repo_root: str = ".",
        export_path: str = "~/models/omnibot_policy.onnx",
    ) -> None:
        if task not in ("nav", "arm"):
            raise ValueError("task must be 'nav' or 'arm'")
        self.task = task
        self.num_envs = num_envs
        self.max_iterations = max_iterations
        self.repo_root = Path(repo_root).expanduser()
        self.export_path = export_path

    def train(
        self, dataset: Any = None, policy: Optional[Policy] = None
    ) -> TrainResult:
        script = self.repo_root / "rl_engine" / "scripts" / f"train_{self.task}.py"
        cmd = [
            sys.executable,
            str(script),
            "--num_envs",
            str(self.num_envs),
            "--max_iterations",
            str(self.max_iterations),
        ]
        proc = subprocess.run(cmd, capture_output=True, text=True)
        if proc.returncode != 0:
            raise RuntimeError(
                f"rl_engine training failed ({proc.returncode}):\n{proc.stderr[-2000:]}"
            )
        return TrainResult(
            trainer=self.name,
            steps=self.max_iterations,
            checkpoint_path=self.export_path,
            metrics={"num_envs": float(self.num_envs)},
        )


@TRAINERS.register("finetune_smolvla")
class FineTuneTrainer(PolicyTrainer):
    """Incremental VLA fine-tuning via ``lerobot_engine/train.py``.
    ``dataset`` is a LeRobot repo_id (export the ReplayDataset first via
    ``ReplayDataset.export_lerobot``)."""

    name = "finetune_smolvla"

    def __init__(
        self,
        steps: int = 20_000,
        repo_root: str = ".",
        base_checkpoint: str = "lerobot/smolvla_base",
        wandb_project: str = "omnibot_smolvla",
    ) -> None:
        self.steps = steps
        self.repo_root = Path(repo_root).expanduser()
        self.base_checkpoint = base_checkpoint
        self.wandb_project = wandb_project

    def train(self, dataset: Any, policy: Optional[Policy] = None) -> TrainResult:
        if not isinstance(dataset, str):
            raise TypeError("FineTuneTrainer expects a LeRobot dataset repo_id string")
        script = self.repo_root / "lerobot_engine" / "train.py"
        cmd = [
            sys.executable,
            str(script),
            "--dataset",
            dataset,
            "--policy",
            self.base_checkpoint,
            "--steps",
            str(self.steps),
            "--wandb-project",
            self.wandb_project,
        ]
        proc = subprocess.run(cmd, capture_output=True, text=True)
        if proc.returncode != 0:
            raise RuntimeError(
                f"lerobot_engine training failed ({proc.returncode}):\n{proc.stderr[-2000:]}"
            )
        return TrainResult(
            trainer=self.name,
            steps=self.steps,
            metrics={},
            checkpoint_path="(see lerobot_engine output dir)",
        )


@TRAINERS.register("noop")
class NoOpTrainer(PolicyTrainer):
    """Counts the data and returns — used for loop dry-runs and tests."""

    name = "noop"

    def train(self, dataset: Any, policy: Optional[Policy] = None) -> TrainResult:
        episodes = _episodes_from(dataset)
        n = sum(len(e) for e in episodes)
        return TrainResult(
            trainer=self.name,
            steps=0,
            final_loss=0.0,
            metrics={"episodes": float(len(episodes)), "transitions": float(n)},
        )
