# robot_episode_dataset

> LeRobot-compatible episode dataset library for robot imitation learning.
> ROS 2 bag → synchronized frames → Parquet + MP4 → PyTorch Dataset.

## Features

- **`TopicSynchronizer`** — nearest-neighbour multi-modal timestamp sync
  (no ROS dependency, works on raw timestamp arrays)
- **`EpisodeDataset`** — PyTorch-compatible loader for LeRobot v2.0 format
- **`DatasetSchema`** — configurable state/action/camera specification
  (`StateSpec`, `ActionSpec`, `CameraConfig`)
- Ready-made OmniBot schema (`OMNIBOT_SCHEMA`) included as an example
  (9-D state/action: 6 arm joints + 3 base velocities)
- Pure Python, no ROS dependency for loading

## Install

```bash
pip install robot-episode-dataset

# With PyTorch support
pip install "robot-episode-dataset[torch]"

# With OpenCV (for video frame loading)
pip install "robot-episode-dataset[viz]"

# Everything (torch + opencv)
pip install "robot-episode-dataset[all]"
```

Requires Python >= 3.10. Core dependencies: `numpy<2.0`, `pyarrow>=14.0`,
`scipy>=1.10`. An optional `[ros]` extra pulls in `rclpy`.

## Usage

### Load a dataset

```python
from robot_episode_dataset import EpisodeDataset

ds = EpisodeDataset(
    root='/data/my_robot_episodes',
    image_keys=['observation.images.bev', 'observation.images.wrist'],
)

print(len(ds))        # number of frames
sample = ds[0]        # dict with parquet columns + requested image keys

# Wrap as a torch.utils.data.Dataset (requires torch):
torch_ds = ds.as_torch_dataset()
```

`EpisodeDataset(root, split=None, transform=None, image_keys=None)`. Pass
`image_keys=None` to skip MP4 decoding for state-only tasks. `image_keys`
require `opencv-python` (the `[viz]` extra).

### Synchronize multi-modal data

```python
from robot_episode_dataset import TopicSynchronizer

sync = TopicSynchronizer(target_fps=10.0, sync_tolerance=0.05)

frames = sync.synchronize({
    'camera_bev':   [(ts_ns, img), ...],   # from /camera/base/bev/image_raw
    'camera_wrist': [(ts_ns, img), ...],   # from /camera/wrist/image_raw
    'state':        [(ts_ns, state), ...],
    'action':       [(ts_ns, action), ...],
})
# frames: list of dicts. Each has a 'timestamp' float (seconds) plus the
# nearest value of every stream within sync_tolerance. Frames missing any
# required_keys stream are dropped (required_keys defaults to all streams).
```

`TopicSynchronizer(target_fps=10.0, sync_tolerance=0.05, required_keys=None)`.
Input timestamps are in nanoseconds; output `timestamp` is in seconds.

### Define your own schema

```python
from robot_episode_dataset import DatasetSchema, StateSpec, ActionSpec, CameraConfig

schema = DatasetSchema(
    robot_name='my_robot',
    state_spec=StateSpec(
        names=['joint_0', 'joint_1', 'vx'],
        ranges=[(-3.14, 3.14), (-3.14, 3.14), (-1.0, 1.0)],
        units=['rad', 'rad', 'm/s'],
    ),
    action_spec=ActionSpec(
        names=['joint_0', 'joint_1', 'vx'],
        ranges=[(-3.14, 3.14), (-3.14, 3.14), (-1.0, 1.0)],
        units=['rad', 'rad', 'm/s'],
    ),
    cameras=[
        # CameraConfig(name, topic, resolution=(height, width), fps, encoding='bgr8')
        CameraConfig('front', '/camera/front/image_raw', (480, 640), 30),
    ],
)
```

Or use the bundled OmniBot schema (mecanum base + SO-101 arm):

```python
from robot_episode_dataset import OMNIBOT_SCHEMA

OMNIBOT_SCHEMA.state_spec.dim          # 9
OMNIBOT_SCHEMA.camera_names            # ['bev', 'wrist']
OMNIBOT_SCHEMA.lerobot_camera_keys     # {'observation.images.bev': ..., ...}
```

## Dataset Format (LeRobot v2.0)

```
<root>/
  meta/
    info.json          # Dataset metadata
    tasks.jsonl        # Task descriptions
    episodes.jsonl     # Per-episode metadata
    stats.json         # Normalization statistics
  data/
    chunk-000/
      episode_000000.parquet   # state, action, timestamp, frame_index
  videos/
    chunk-000/
      observation.images.bev/episode_000000.mp4
      observation.images.wrist/episode_000000.mp4
```

## License

Apache-2.0
