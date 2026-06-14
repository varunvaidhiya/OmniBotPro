# OmniBot Data Engine

Tooling for collecting, converting, validating, and loading robot demonstrations
in **LeRobot v2.0 format** (Parquet + MP4 video) for SmolVLA training.

## Format

All datasets use the LeRobot v2.0 layout:

```
dataset/
  meta/
    info.json          ← fps, total_episodes, total_frames, feature shapes
    tasks.jsonl        ← task index → task string
    episodes.jsonl     ← episode index → task list, length
    stats.json         ← per-feature mean/std/min/max for normalisation
  data/
    chunk-000/
      episode_000000.parquet   ← state, action, timestamps (one row per frame)
  videos/
    chunk-000/
      observation.images.front/episode_000000.mp4
      observation.images.wrist/episode_000000.mp4
```

State and action are the unified **9-D** mobile-manipulation spec (6 arm joints
+ 3 base velocities), defined in `schema/constants.py`
(`MOBILE_MANIP_STATE_SPEC` / `MOBILE_MANIP_ACTION_SPEC`). Camera streams written
to the dataset are `observation.images.front` and `observation.images.wrist`.

## Setup

```bash
pip install -e .          # installs the `vla_data_engine` package
# or: pip install -r requirements.txt
```

## Usage

### ROS 2 bag → dataset (single episode)
```bash
python -m data_engine.ingestion.bag_to_omnibot \
    --bag /data/bags/ep000 \
    --dataset /data/lerobot/pick_place \
    --task "pick up the red cube" \
    --fps 30
```

### Batch convert
```bash
python -m data_engine.scripts.ingest_dataset \
    --input-dir /data/bags \
    --output-dir /data/lerobot/pick_place \
    --task "pick up the red cube"
```

### Validate
```bash
python -m data_engine.scripts.validate_dataset --dataset /data/lerobot/pick_place
```

### Visualise an episode
```bash
python -m data_engine.visualization.visualize_episode \
    --dataset /data/lerobot/pick_place \
    --episode 0
```

See `TRAINING_GUIDE.md` for the full end-to-end workflow.
