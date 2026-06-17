# ros2_bev_stitcher — OhhO View

> **Four cameras. One smart view.**
>
> Multi-camera **Bird's Eye View** compositor for ROS 2.
> Fuse base-mounted cameras into a single calibrated surround BEV
> your robot and operators can both rely on — CPU-only, ROS 2-ready.

Subscribes to N perspective cameras, warps each frame onto a shared top-down
canvas via per-camera homography matrices, distance-weighted alpha-blends
overlapping regions, and publishes a single unified BEV image — ready for VLA
training, navigation, or operator display.

## Features

- Configurable camera list (works with 1–8+ cameras)
- **Two calibration methods:**
  - **Geometric IPM** — compute homographies from camera extrinsic/intrinsic
    params (position + orientation in base_link), no checkerboard needed
  - **Checkerboard** — one-shot OpenCV checkerboard calibration via `bev_calibrate`
- **Interactive calibration UI** — live trackbar wizard to fine-tune camera
  poses visually (`bev_cal_ui`)
- Per-camera homography-based perspective warp (OpenCV `warpPerspective`)
- Distance-weighted blending in overlap zones
- Graceful fallback to tiled layout when uncalibrated
- Optional stitch-timing diagnostics on `/diagnostics`
- All parameters exposed as ROS 2 params — no code changes needed

## Quick Start

```bash
# 1. Build
cd ~/ros2_ws/src && ln -s /path/to/ros2_bev_stitcher .
cd ~/ros2_ws && colcon build --packages-select ros2_bev_stitcher
source install/setup.bash

# 2a. Quick setup with IPM (geometric, no checkerboard)
#     Computes homographies from default camera poses — good for initial bring-up
ros2 run ros2_bev_stitcher bev_ipm \
    --camera-names front rear left right \
    --output ~/bev_calibration.npz

# 2b. Interactive calibration (recommended)
#     Live BEV view with trackbars to fine-tune each camera's 6-DOF pose
ros2 run ros2_bev_stitcher bev_cal_ui \
    --camera-names front rear left right

# 2c. Checkerboard calibration (alternative)
#     Place a 9×6 (inner corners) checkerboard flat on the ground, visible to
#     all cameras, then run:
ros2 run ros2_bev_stitcher bev_calibrate \
    --camera-names front rear left right \
    --square-size 0.025 \
    --output ~/bev_calibration.npz

# 3. Launch the stitcher (auto-selects best calibration)
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py

# 4. View result
ros2 run rqt_image_view rqt_image_view
# → select /camera/bev/image_raw
```

## Calibration Workflow

### Recommended: Geometric IPM + Interactive Tuning

1. Run `bev_ipm` once to generate an initial calibration from default poses
2. Run `bev_cal_ui` to interactively fine-tune each camera's position and
   orientation with live trackbars
3. Save the fine-tuned calibration and use it with the stitcher

### Run the stitcher in IPM mode

```bash
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py \
    calibration_mode:=ipm
```

The stitcher computes homographies from camera pose parameters at startup.
No .npz file needed — set per-camera params and the BEV is computed
geometrically.

### Using a params file

```bash
ros2 run ros2_bev_stitcher bev_stitcher \
    --ros-args --params-file config/bev_params.yaml
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
| `calibration_mode` | `auto` | `checkerboard` / `ipm` / `auto` |
| `calibration_file` | `~/bev_calibration.npz` | Per-camera homography file |
| `pixels_per_meter` | `80.0` | IPM scale: canvas pixels per world metre |
| `ground_z` | `0.0` | Ground plane height in base_link (m) |
| `output_frame_id` | `bev_frame` | TF frame written into the image header |
| `publish_diagnostics` | `False` | Publish rolling stitch timing to `/diagnostics` at 1 Hz |

**Per-camera IPM parameters** (only used when `calibration_mode` is `ipm` or `auto`):

| Parameter | Default | Description |
|---|---|---|
| `<camera>.position` | varies | Camera position [x, y, z] in base_link (m) |
| `<camera>.orientation_rpy` | varies | Camera orientation [roll, pitch, yaw] (rad) |
| `<camera>.fx` | `320.0` | Focal length x (pixels) |
| `<camera>.fy` | `320.0` | Focal length y (pixels) |
| `<camera>.cx` | `320.0` | Principal point x |
| `<camera>.cy` | `240.0` | Principal point y |

Config: [`config/bev_params.yaml`](config/bev_params.yaml) (load it with
`--ros-args --params-file`; note it is not loaded by the launch file by
default).

### Launch arguments

`bev_stitcher.launch.py` exposes parameters as launch arguments:
`canvas_size`, `output_width`, `output_height`, `src_width`, `src_height`,
`publish_hz`, `calibration_file`, `calibration_mode`, `pixels_per_meter`,
`ground_z`, `output_topic`.

```bash
# Launch in IPM mode with custom params
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py \
    calibration_mode:=ipm pixels_per_meter:=100.0

# Launch with checkerboard calibration
ros2 launch ros2_bev_stitcher bev_stitcher.launch.py \
    calibration_mode:=checkerboard \
    calibration_file:=/path/to/my_calibration.npz
```

## Tools

| Tool | Description |
|---|---|
| `bev_stitcher` | Main stitcher node — subscribe + warp + blend + publish |
| `bev_ipm` | One-shot IPM calibrator — computes homographies from camera poses |
| `bev_cal_ui` | Interactive calibration UI — live BEV + trackbars per camera |
| `bev_calibrate` | Legacy checkerboard calibration — detects corners, computes RANSAC homographies |

### bev_ipm

```bash
ros2 run ros2_bev_stitcher bev_ipm \
    --camera-names front rear left right \
    --canvas-size 800 \
    --pixels-per-meter 80.0 \
    --output ~/bev_calibration.npz

# Override camera positions on the CLI:
ros2 run ros2_bev_stitcher bev_ipm \
    --positions front:0.25:0:0.12 rear:-0.25:0:0.12 left:0:0.18:0.12 right:0:-0.18:0.12

# Override orientations (r:p:y in radians):
ros2 run ros2_bev_stitcher bev_ipm \
    --orientations front:0:0.4:0 rear:0:-0.4:3.14 left:0:0.4:1.57 right:0:0.4:-1.57
```

### bev_cal_ui

Interactive calibration wizard. Shows:
- Live fused BEV from current camera images
- Trackbars to adjust each camera's 6-DOF pose (position + orientation)
- Trackbars for camera intrinsics (fx, fy, cx, cy)
- Global scale and ground-plane adjustment
- Save to .npz and human-readable params file

```bash
ros2 run ros2_bev_stitcher bev_cal_ui \
    --camera-names front rear left right \
    --canvas-size 800 \
    --output ~/bev_calibration.npz
```

Controls:
- Select camera with the top trackbar
- Adjust its 6-DOF pose with position and orientation trackbars
- **s** — save calibration
- **q** or **ESC** — quit

### bev_calibrate

Legacy checkerboard-based calibration. See original docs below.

## Algorithm

```
# IPM mode:
For each camera:
  K = camera intrinsics
  R, t = camera pose (world → camera)
  Ground plane: z = ground_z in world frame
  H = K @ [r1, r2, r3*ground_z + t]    → world-to-image
  H_inv = inverse(H)                     → image-to-world
  H_canvas = scale @ translate           → world-to-canvas
  H_final = H_canvas @ H_inv            → image-to-canvas

For each camera:
  img → warpPerspective(H_final) → warped_canvas
  blend_weight = warpPerspective(ones, H_final)

Final BEV = Σ(warped_i × weight_i) / Σ(weight_i)
```

## License

Apache-2.0
