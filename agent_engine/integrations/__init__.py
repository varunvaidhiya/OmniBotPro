"""Edge adapters wiring external systems into the harness ports.

``learning_engine`` adapters (verifier, reflector, episodic memory, continual
learning) are imported lazily by callers — importing this package does not
require ``learning_engine`` to be installed.
"""

from __future__ import annotations

__all__ = [
    "WorldStateVerifier",
    "EvaluatorReflector",
    "ReplayMemorySource",
    "ContinualLearningClosure",
]


def __getattr__(name: str):  # lazy re-export; avoids importing learning_engine eagerly
    if name in __all__:
        from . import learning_engine as _le

        return getattr(_le, name)
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
