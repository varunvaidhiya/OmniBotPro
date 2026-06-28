# Install OhhO OS

OhhO OS is a Python package. Install the core, then add only the extras you need.

## Requirements

- **Python** 3.10 or newer
- **OS** — Windows, macOS or Linux for the no-ROS runtime; Ubuntu 24.04 for the
  ROS 2 runtime
- A robot, or none at all — a built-in simulator and replay mode let you run
  everything without hardware

## Quick install

```bash
pip install 'ohho-os[base]'
```

Or use the one-line installer (wraps `uv` for a fast, isolated environment):

```bash
curl -fsSL https://ohho.ai/install.sh | sh
```

## Extras

Install only what your robot and workflow need:

| Extra | Adds |
|---|---|
| `base` | Core runtime, capability model, simulator, CLI |
| `unitree` | Unitree DDS adapter (G1, Go2, H1, B2 …) |
| `dji` | DJI MAVLink adapter (drones) |
| `ros2` | ROS 2 runtime backend (Nav2, SLAM, MoveIt 2) |
| `train` | Training pipelines (VLA / imitation / RL) |
| `serve` | Inference server for serving policies over REST |
| `all` | Everything above |

```bash
# A no-ROS Unitree quadruped with training:
pip install 'ohho-os[base,unitree,train]'

# The full ROS 2 experience:
pip install 'ohho-os[all]'
```

## Verify

```bash
ohho doctor
```

`ohho doctor` reports your Python version, which runtimes are available
(native and/or ROS 2), which adapters are installed, and what hardware it can
see — then recommends a runtime for your setup.

## Next

- [Quickstart](quickstart.md) — connect to a robot and drive it.
- [Runtimes: ROS vs no-ROS](runtimes.md) — pick the right backend.
