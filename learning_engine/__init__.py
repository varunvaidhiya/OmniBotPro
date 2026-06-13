"""OmniBot post-training & continual-learning framework.

Architecture-first: every layer (collection, simulation, rewards,
evaluation, replay, policy learning, verification, continual learning) is a
pluggable component behind the interfaces in ``learning_engine.core``.
See ARCHITECTURE.md for the full design and README.md for quickstarts.

Only ``core`` is imported eagerly; subpackages with optional heavy
dependencies (torch, lerobot, isaaclab, rclpy, anthropic) import lazily.
"""

from . import core
from .core import (  # noqa: F401 — convenience re-exports
    DataSource,
    Episode,
    EpisodeMeta,
    Step,
    TaskOutcome,
    Transition,
)

__version__ = "0.1.0"
__all__ = [
    "core",
    "DataSource",
    "Episode",
    "EpisodeMeta",
    "Step",
    "TaskOutcome",
    "Transition",
    "__version__",
]
