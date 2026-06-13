"""Name → class registries so YAML configs can assemble the system.

Usage::

    from learning_engine.core.registry import REWARD_TERMS

    @REWARD_TERMS.register("goal_distance")
    class GoalDistanceReward(RewardTerm): ...

    term = REWARD_TERMS.create("goal_distance", weight=0.5)
"""

from __future__ import annotations

from typing import Any, Callable, Dict, List, Type, TypeVar

T = TypeVar("T")


class Registry:
    def __init__(self, kind: str) -> None:
        self.kind = kind
        self._items: Dict[str, type] = {}

    def register(self, name: str) -> Callable[[Type[T]], Type[T]]:
        def deco(cls: Type[T]) -> Type[T]:
            if name in self._items:
                raise KeyError(f"{self.kind} '{name}' already registered")
            self._items[name] = cls
            return cls

        return deco

    def get(self, name: str) -> type:
        try:
            return self._items[name]
        except KeyError:
            raise KeyError(
                f"Unknown {self.kind} '{name}'. Available: {sorted(self._items)}"
            ) from None

    def create(self, name: str, **kwargs: Any) -> Any:
        return self.get(name)(**kwargs)

    def names(self) -> List[str]:
        return sorted(self._items)


COLLECTORS = Registry("collector")
ENVS = Registry("simulation env")
REWARD_TERMS = Registry("reward term")
REWARD_MODELS = Registry("reward model")
EVALUATORS = Registry("evaluator")
POLICIES = Registry("policy")
TRAINERS = Registry("trainer")
PLAN_CHECKS = Registry("plan check")
