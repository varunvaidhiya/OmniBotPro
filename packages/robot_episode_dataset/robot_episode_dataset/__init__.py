"""
robot_episode_dataset
~~~~~~~~~~~~~~~~~~~~~
LeRobot-compatible episode dataset library for robot imitation learning.

Quick start::

    from robot_episode_dataset import EpisodeDataset, TopicSynchronizer

    # Load a dataset
    ds = EpisodeDataset('/path/to/dataset', image_keys=['observation.images.bev'])
    sample = ds[0]

    # Synchronize ROS bag data
    sync = TopicSynchronizer(target_fps=10.0)
    frames = sync.synchronize({'camera': [...], 'state': [...]})
"""

from .sync import TopicSynchronizer
from .schema import (
    StateSpec,
    ActionSpec,
    CameraConfig,
    DatasetSchema,
    OMNIBOT_SCHEMA,
)
from .loader import EpisodeDataset

__version__ = "1.0.0"
__all__ = [
    "TopicSynchronizer",
    "StateSpec",
    "ActionSpec",
    "CameraConfig",
    "DatasetSchema",
    "OMNIBOT_SCHEMA",
    "EpisodeDataset",
]
