# ros2_bev_stitcher

> Multi-camera **Bird's Eye View** compositor for ROS 2.

Subscribes to N perspective cameras, warps each frame onto a shared top-down
canvas via per-camera homography matrices, distance-weighted alpha-blends
overlapping regions, and publishes a single unified BEV image — ready for VLA
training, navigation, or operator display.

## Features

- Configurable camera list (works with 1–8+ cameras)
- Per-camera homography-based perspective warp (OpenCV `warpPerspective`)
- Distance-weighted blending in overlap zones
- One-command checkerboard calibration tool (`bev_calibrate`)
- Graceful fallback to tiled layout when uncalibrated
- Optional stitch-timing diagnostics on `/diagnostics`
- All parameters exposed as ROS 2 params — no code changes needed

## Quick Start

```bash
# 1. Build
cd ~/ros2_ws/src && ln -s /path/to/ros2_bev_stitcher .
cd ~/ros2_ws && colcon build --packages-select ros2_bev_stitcher
source install/setup.bash

# 2. Calibrate (one-time)
#    Place a 9×6 (inner corners) checkerboard flat on the ground, visible to
#    all cameras, then run:
ros2 run ros2_bev_stitcher bev_calibrate \
    --camera-names front rear left right \
    --square-size 0.025 \
    --output ~/bev_calibration.npz

# 3. Launch
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py

# 4. View result
ros2 run rqt_image_view rqt_image_view
# → select /camera/bev/image_raw
```

## Node

`bev_stitcher` (executable; node name `bev_stitcher`, entry point
`ros2_bev_stitcher.bev_stitcher_node:main`).

### Topics

| Direction | Topic | Type |
|---|---|---|
| Subscribe | `/camera/{name}/image_raw` (one per camera in `camera_names`) | `sensor_msgs/Image` |
| Publish | `/camera/bev/image_raw` (`output_topic`) | `sensor_msgs/Image` |
| Publish | `/diagnostics` (only when `publish_diagnostics:=true`) | `diagnostic_msgs/DiagnosticArray` |

The subscribe topic is built from `input_topic_pattern` with `{name}`
substituted for each entry in `camera_names`. Published images use `rgb8`
encoding and `output_frame_id` in the header.

### Parameters

| Parameter | Default | Description |
|---|---|---|
| `camera_names` | `['front','rear','left','right']` | Camera names (one subscription + one homography key each) |
| `input_topic_pattern` | `/camera/{name}/image_raw` | Input topic pattern (`{name}` substituted) |
| `output_topic` | `/camera/bev/image_raw` | Published BEV topic |
| `canvas_size` | `800` | Internal compositing canvas (px, square) |
| `output_width` | `800` | Final output image width (px) |
| `output_height` | `800` | Final output image height (px) |
| `src_width` | `640` | Expected input image width (px); inputs are resized to this |
| `src_height` | `480` | Expected input image height (px) |
| `publish_hz` | `30.0` | Publish rate (Hz) |
| `calibration_file` | `~/bev_calibration.npz` | Per-camera homography file |
| `output_frame_id` | `bev_frame` | TF frame written into the image header |
| `publish_diagnostics` | `False` | Publish rolling stitch timing to `/diagnostics` at 1 Hz |

Config: [`config/bev_params.yaml`](config/bev_params.yaml) (load it with
`--ros-args --params-file`; note it is not loaded by the launch file by
default).

### Launch arguments

`bev_stitcher.launch.py` exposes a subset of the parameters as launch
arguments: `canvas_size`, `output_width`, `output_height`, `src_width`,
`src_height`, `publish_hz`, `calibration_file`, `output_topic`.

```bash
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py \
    output_topic:=/camera/bev/image_raw publish_hz:=15.0
```

(`camera_names`, `input_topic_pattern`, `output_frame_id`, and
`publish_diagnostics` are not launch arguments — set them via a params file or
`ros2 run ... --ros-args -p`.)

## Calibration

`bev_calibrate` (entry point `ros2_bev_stitcher.bev_calibrate:main`) is a
one-shot node. It captures one frame per camera, detects checkerboard corners,
maps them to a shared ground-plane coordinate system (origin at canvas centre,
1 m → half the canvas), computes a 3×3 RANSAC homography per camera, shows a
warped preview for ~2 s, and saves all homographies to a `.npz` file keyed by
camera name. `bev_stitcher` loads this file automatically on startup if it
exists, otherwise it falls back to a tiled layout.

```bash
ros2 run ros2_bev_stitcher bev_calibrate \
    --camera-names front rear left right \
    --input-topics /camera/front/image_raw /camera/rear/image_raw \
                   /camera/left/image_raw /camera/right/image_raw \
    --checkerboard-cols 9 --checkerboard-rows 6 \
    --square-size 0.025 \
    --canvas-size 800 \
    --output ~/bev_calibration.npz
```

| Argument | Default | Description |
|---|---|---|
| `--camera-names` | `front rear left right` | Camera names (homography keys) |
| `--input-topics` | `/camera/<name>/image_raw` per camera | Input topic per camera |
| `--checkerboard-cols` | `9` | Inner-corner columns |
| `--checkerboard-rows` | `6` | Inner-corner rows |
| `--square-size` | `0.025` | Checkerboard square size (metres) |
| `--canvas-size` | `800` | Canvas size used for the homography (must match the node) |
| `--output` | `~/bev_calibration.npz` | Output file |

## Algorithm

```
For each camera:
  img → warpPerspective(H) → warped_canvas
  blend_weight = warpPerspective(ones, H)   (overlap coverage map)

Final BEV = Σ(warped_i × weight_i) / Σ(weight_i)   (per pixel, where Σweight > 0)
```

## License

Apache-2.0
