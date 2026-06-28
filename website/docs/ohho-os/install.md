# Install OhhO OS

OhhO OS is a Python package. Install the core, then add only the extras you need.

## Requirements

- **Python** 3.10 or newer
- **OS** — Windows, macOS or Linux for the no-ROS runtime; Ubuntu 24.04 for the
  ROS 2 runtime
- A robot, or none at all — a built-in simulator lets you run everything
  without hardware

## Quick install

```bash
pip install 'ohho-os[base]'
```

The base install is **dependency-free** — core runtime, capability model,
simulator, agent brain (ScriptedBrain fallback), and CLI run on the standard
library alone, on any OS.

## Extras

Install only what your robot and workflow need:

| Extra | Adds | Dependencies |
|---|---|---|
| `base` | Core runtime, capability model, simulator, CLI, ScriptedBrain | none |
| `serial` | Yahboom serial adapter (OmniBot base) | `pyserial>=3.5` |
| `arm` | Feetech arm adapter (SO-101, OmniBot arm) | `lerobot>=0.1` |
| `agent` | Real agent brain (Claude tool-calling) | `numpy>=1.24`, `anthropic>=0.40` |
| `data` | LeRobot Parquet dataset writer | `pyarrow>=14.0` |
| `unitree` | Unitree DDS adapter (G1, Go2, H1, B2) | `cyclonedds>=0.10` |
| `dji` | DJI MAVLink adapter (drones) | `pymavlink>=2.4` |
| `serve` | Inference server (FastAPI + uvicorn) | `fastapi>=0.110`, `uvicorn>=0.27` |
| `train` | Training pipelines (VLA fine-tuning) | `torch>=2.1`, `numpy>=1.24` |
| `ros2` | ROS 2 runtime backend | rclpy (ships with ROS 2 Jazzy) |
| `yaml` | Load robot manifests from YAML | `pyyaml>=6.0` |
| `all` | Everything above | all dependencies |

```bash
# A no-ROS Unitree quadruped with training:
pip install 'ohho-os[base,unitree,train]'

# OmniBot with arm + agent + data collection:
pip install 'ohho-os[serial,arm,agent,data]'

# The full ROS 2 experience (on Ubuntu 24.04 with Jazzy):
pip install 'ohho-os[all]'
```

## Verify

```bash
ohho doctor
```

`ohho doctor` reports your Python version, which runtimes are available
(native and/or ROS 2), which adapters are installed, the compute device, and
the built-in robots.

```
OhhO OS 1.0.0
  python      : 3.12.4
  runtimes    : native
  adapters    : sim, serial, feetech  (others via extras)
  device      : cpu
  robots      : omnibot, sim, unitree-go2
```

## Install from source (development)

```bash
git clone https://github.com/varunvaidhiya/OmniBotPro.git
cd OmniBotPro
pip install -e sdk              # editable install
python -m unittest discover -s sdk/tests   # 151 tests should pass
```

## Next

- [Setup Guide](setup.md) — a complete walkthrough from install to moving robot.
- [Quickstart](quickstart.md) — connect to a robot and drive it.
- [Runtimes: ROS vs no-ROS](runtimes.md) — pick the right backend.
