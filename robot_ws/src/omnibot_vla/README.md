# omnibot_vla

Integrates the [OpenVLA](https://github.com/openvla/openvla) model for
Vision-Language-Action control of the OmniBot base.

## Overview

`vla_node` bridges ROS 2 and the OpenVLA model:

- **Input**: RGB image on `/image_raw` (remapped from a camera topic) +
  text prompt on `/vla/prompt`
- **Output**: base velocity on `/cmd_vel/vla`

The node publishes to `/cmd_vel/vla` (not `/cmd_vel` directly) so the
`cmd_vel_mux` in `omnibot_hybrid` can select it when `control_mode == "vla"`.
Inference runs on a 1 Hz timer. The OpenVLA 7-DOF action is mapped to a base
`Twist`: `action[0] → linear.x`, `action[1] → linear.y`, `action[5] → angular.z`
(`unnorm_key="bridge_orig"`).

## Hardware Requirements

Run on a desktop PC with a dedicated NVIDIA GPU, **not** the Raspberry Pi.
- **VRAM**: 16 GB+ recommended (or run with `load_in_4bit:=True` for ~8 GB).
- **RAM**: 32 GB+ system RAM.

## Dependencies

Not included in a standard ROS 2 desktop install:

```bash
pip3 install torch torchvision torchaudio
pip3 install transformers accelerate bitsandbytes protobuf scipy
```

4-bit loading additionally requires `bitsandbytes`; the model uses
`flash_attention_2`.

## Usage

### Launch (on the desktop GPU PC)

```bash
ros2 launch omnibot_vla vla_desktop.launch.py
ros2 launch omnibot_vla vla_desktop.launch.py image_topic:=/camera/wrist/image_raw
```

The `image_topic` launch argument (default `/camera/front/image_raw`) is
remapped to the node's `/image_raw` subscription.

### Prompt

```bash
ros2 topic pub --once /vla/prompt std_msgs/msg/String "data: 'Find the red cup'"
```

## Node: `vla_node`

| Parameter | Default | Description |
|---|---|---|
| `model_path` | `openvla/openvla-7b` | Path or HuggingFace ID. `trust_remote_code` is enabled only for `openvla/*` IDs or local dirs. |
| `device` | `cuda` | Inference device |
| `load_in_4bit` | `False` | 4-bit quantization for limited VRAM |
| `publish_diagnostics` | `False` | Publish rolling inference timing to `/diagnostics` at 1 Hz |

**Subscribes**: `/image_raw` (`sensor_msgs/Image`), `/vla/prompt` (`std_msgs/String`)
**Publishes**: `/cmd_vel/vla` (`geometry_msgs/Twist`); `/diagnostics`
(`diagnostic_msgs/DiagnosticArray`) when `publish_diagnostics:=True`

> The `/image_raw` topic name is hardcoded in the node; use the launch file's
> `image_topic` remap to feed a different camera.
