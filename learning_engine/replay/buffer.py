"""Experience replay — in-memory transition buffers and an episodic store.

- ``UniformReplayBuffer``      — ring buffer, uniform sampling.
- ``PrioritizedReplayBuffer``  — proportional PER (Schaul et al. 2016):
  P(i) ∝ priorityᵢ^α, with importance-sampling weights w ∝ (N·P(i))^−β.
- ``EpisodicReplayStore``      — episode-level prioritization over the
  on-disk ReplayDataset, with outcome-stratified sampling
  (success / near-success / failure ratios).
"""

from __future__ import annotations

import random
from typing import Dict, List, Optional, Tuple

import numpy as np

from ..core.interfaces import ReplayBuffer
from ..core.types import Episode, TaskOutcome, Transition
from ..data.replay_dataset import ReplayDataset


class UniformReplayBuffer(ReplayBuffer):
    def __init__(self, capacity: int = 100_000, seed: Optional[int] = None) -> None:
        self.capacity = capacity
        self._data: List[Transition] = []
        self._next = 0
        self._rng = random.Random(seed)

    def add(self, transition: Transition, priority: Optional[float] = None) -> None:
        if len(self._data) < self.capacity:
            self._data.append(transition)
        else:
            self._data[self._next] = transition
        self._next = (self._next + 1) % self.capacity

    def add_episode(self, episode: Episode) -> None:
        for t in episode.transitions():
            self.add(t)

    def sample(self, batch_size: int) -> List[Transition]:
        if not self._data:
            return []
        return [self._rng.choice(self._data) for _ in range(batch_size)]

    def __len__(self) -> int:
        return len(self._data)


class PrioritizedReplayBuffer(ReplayBuffer):
    """Proportional prioritized replay. O(n) sampling via numpy — fine for
    the ≤1e6 transitions this robot realistically holds in RAM; swap in a
    sum-tree if profiling ever says otherwise."""

    def __init__(
        self,
        capacity: int = 100_000,
        alpha: float = 0.6,
        beta: float = 0.4,
        epsilon: float = 1e-3,
        seed: Optional[int] = None,
    ) -> None:
        self.capacity = capacity
        self.alpha = alpha
        self.beta = beta
        self.epsilon = epsilon
        self._data: List[Transition] = []
        self._priorities = np.zeros(capacity, dtype=np.float64)
        self._next = 0
        self._rng = np.random.default_rng(seed)

    def add(self, transition: Transition, priority: Optional[float] = None) -> None:
        if priority is None:
            # New experience gets max priority so it is sampled at least once.
            priority = self._priorities[: len(self._data)].max() if self._data else 1.0
        if len(self._data) < self.capacity:
            self._data.append(transition)
        else:
            self._data[self._next] = transition
        self._priorities[self._next] = (abs(priority) + self.epsilon) ** self.alpha
        self._next = (self._next + 1) % self.capacity

    def add_episode(self, episode: Episode, priority: Optional[float] = None) -> None:
        for t in episode.transitions():
            self.add(t, priority)

    def sample(self, batch_size: int) -> List[Transition]:
        transitions, _, _ = self.sample_with_weights(batch_size)
        return transitions

    def sample_with_weights(
        self, batch_size: int
    ) -> Tuple[List[Transition], np.ndarray, np.ndarray]:
        """Returns (transitions, indices, importance_weights). Trainers that
        correct PER bias should multiply losses by the weights and call
        ``update_priorities(indices, new_td_errors)`` afterwards."""
        n = len(self._data)
        if n == 0:
            return [], np.array([], dtype=int), np.array([])
        p = self._priorities[:n]
        probs = p / p.sum()
        idx = self._rng.choice(n, size=batch_size, p=probs)
        weights = (n * probs[idx]) ** (-self.beta)
        weights /= weights.max()
        return [self._data[i] for i in idx], idx, weights

    def update_priorities(self, indices: np.ndarray, priorities: np.ndarray) -> None:
        self._priorities[indices] = (np.abs(priorities) + self.epsilon) ** self.alpha

    def __len__(self) -> int:
        return len(self._data)


class EpisodicReplayStore:
    """Outcome-stratified episode sampling over the on-disk ReplayDataset.

    Failure and near-success trajectories carry most of the learning signal
    for offline RL and self-improvement, so sampling ratios are explicit::

        store = EpisodicReplayStore(dataset,
                                    ratios={"success": 0.5,
                                            "near_success": 0.3,
                                            "failure": 0.2})
    """

    DEFAULT_RATIOS = {
        TaskOutcome.SUCCESS.value: 0.5,
        TaskOutcome.NEAR_SUCCESS.value: 0.3,
        TaskOutcome.FAILURE.value: 0.2,
    }

    def __init__(
        self,
        dataset: ReplayDataset,
        ratios: Optional[Dict[str, float]] = None,
        recency_half_life: int = 200,
        seed: Optional[int] = None,
    ) -> None:
        self.dataset = dataset
        self.ratios = dict(ratios or self.DEFAULT_RATIOS)
        self.recency_half_life = recency_half_life
        self._rng = np.random.default_rng(seed)

    def sample_episode_ids(self, n: int) -> List[str]:
        picks: List[str] = []
        for outcome_val, ratio in self.ratios.items():
            ids = self.dataset.episode_ids(outcome=TaskOutcome(outcome_val))
            if not ids:
                continue
            k = max(1, round(n * ratio))
            # Recency bias: newer episodes (later in the sorted list) are
            # exponentially more likely.
            ranks = np.arange(len(ids), dtype=np.float64)
            w = np.exp((ranks - len(ids)) * np.log(2) / self.recency_half_life)
            picks.extend(
                self._rng.choice(
                    ids, size=min(k, len(ids)), replace=False, p=w / w.sum()
                )
            )
        self._rng.shuffle(picks)  # type: ignore[arg-type]
        return list(picks)[:n]

    def sample_episodes(self, n: int) -> List[Episode]:
        return [self.dataset.get(eid) for eid in self.sample_episode_ids(n)]

    def fill_transition_buffer(self, buffer: ReplayBuffer, n_episodes: int) -> int:
        """Load sampled episodes into a transition buffer; success episodes
        get a priority bump proportional to their evaluated score."""
        count = 0
        for ep in self.sample_episodes(n_episodes):
            priority = 0.5 + ep.meta.success_score
            for t in ep.transitions():
                buffer.add(t, priority)
                count += 1
        return count
