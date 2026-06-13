"""RewardEngine — composes pluggable RewardTerms into a multi-objective
reward and annotates transitions/episodes with full breakdowns.

Design:
- terms produce raw values; the engine stores both raw values and weights in
  ``RewardBreakdown`` so offline re-weighting never requires recollection;
- stateful terms (progress, smoothness) are reset at episode boundaries;
- ``annotate_episode`` writes breakdowns back onto ``Step.reward``, which is
  what ReplayDataset persists.
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional, Sequence

from ..core.interfaces import RewardTerm
from ..core.registry import REWARD_TERMS
from ..core.types import Episode, RewardBreakdown, Transition
from . import terms as _builtin_terms  # noqa: F401 — populate the registry


class RewardEngine:
    def __init__(self, terms: Sequence[RewardTerm]) -> None:
        names = [t.name for t in terms]
        if len(set(names)) != len(names):
            raise ValueError(f"duplicate reward term names: {names}")
        self.terms = list(terms)

    @classmethod
    def from_config(cls, term_specs: Sequence[Dict[str, Any]]) -> "RewardEngine":
        """Build from config: ``[{name: goal_progress, weight: 1.0, ...}]``."""
        built = []
        for spec in term_specs:
            kwargs = dict(spec)
            built.append(REWARD_TERMS.create(kwargs.pop("name"), **kwargs))
        return cls(built)

    def reset(self) -> None:
        for t in self.terms:
            t.reset()

    def compute(
        self, transition: Transition, context: Optional[Dict[str, Any]] = None
    ) -> RewardBreakdown:
        context = context or {}
        breakdown = RewardBreakdown(
            terms={t.name: float(t.compute(transition, context)) for t in self.terms},
            weights={t.name: t.weight for t in self.terms},
        )
        transition.reward_breakdown = breakdown
        transition.reward = breakdown.total
        return breakdown

    def annotate_episode(
        self, episode: Episode, context: Optional[Dict[str, Any]] = None
    ) -> List[RewardBreakdown]:
        """Recompute rewards for every step of an episode (offline
        annotation of demos / execution logs that arrived without rewards)."""
        self.reset()
        breakdowns = []
        for step, transition in zip(episode.steps, episode.transitions()):
            b = self.compute(transition, context)
            step.reward = b
            breakdowns.append(b)
        return breakdowns
