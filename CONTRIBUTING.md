# Contributing to OmniBot

Thanks for your interest in OmniBot! This project welcomes two kinds of contributors:

- **Engineering contributors** — ROS 2 nodes, drivers, simulation, CI, Android app
- **Research contributors** — benchmarking VLA models, collecting demonstrations, writing up results for the paper

Both tracks are equally valued. Read the section that applies to you.

---

## Table of Contents

1. [Before You Start](#before-you-start)
2. [Development Setup](#development-setup)
3. [Engineering Contributions](#engineering-contributions)
4. [Research & Benchmarking Contributions](#research--benchmarking-contributions)
5. [Submitting a Pull Request](#submitting-a-pull-request)
6. [Issue Labels](#issue-labels)
7. [Getting Help](#getting-help)

---

## Before You Start

- Check [open issues](../../issues) and [pull requests](../../pulls) to avoid duplicate work.
- For significant changes, open an issue first to discuss the approach.
- Read [CLAUDE.md](./CLAUDE.md) — it documents every package, parameter, known bug, and architectural decision. This is the single source of truth for the codebase.
- Review [TESTING_GUIDE.md](./TESTING_GUIDE.md) before writing tests.

---

## Development Setup

### Prerequisites

| Requirement | Version |
|---|---|
| Ubuntu | 24.04 (Noble) |
| ROS 2 | Jazzy |
| Python | 3.10+ |
| GPU (research/VLA only) | NVIDIA, ≥ 16 GB VRAM |

Hardware is **not required** for most engineering contributions — Gazebo simulation covers the majority of development scenarios.

### Clone and install

```bash
git clone https://github.com/varunvaidhiya/OmniBotPro.git
cd OmniBotPro

# Install standalone Python packages in editable mode
pip install -e packages/yahboom_ros2
pip install -e packages/vla_serve
pip install -e packages/robot_episode_dataset

# Install ROS 2 dependencies
cd robot_ws
rosdep install --from-paths src --ignore-src -y

# Build
colcon build --symlink-install
source install/setup.bash
```

### Quick start with Docker Compose

```bash
cp .env.example .env          # fill in your IPs and optionally set VLA_API_KEY
docker compose up robot       # starts ROS 2 stack + ROSBridge
docker compose up vla         # starts VLA inference server (requires NVIDIA GPU)
docker compose up             # starts everything
```

### Install pre-commit hooks

```bash
pip install pre-commit
pre-commit install            # runs ruff + mypy on every commit
pre-commit run --all-files    # check the whole repo right now
```

### Run tests locally before pushing

```bash
# ROS 2 workspace tests
cd robot_ws
colcon test
colcon test-result --verbose

# Python packages with coverage
pytest packages/ vla_engine/tests/ data_engine/tests/ \
      --cov=packages --cov=vla_engine --cov=data_engine \
      --cov-report=term-missing

# VLA engine only
cd vla_engine && pytest tests/ -v

# Data engine only
cd data_engine && pytest tests/ -v
```

### vla_serve API key (local dev)

Leave `VLA_API_KEY` empty in `.env` to disable authentication locally.
Set it to a random 32-byte hex string for any shared or networked deployment:

```bash
openssl rand -hex 32   # generate a key
# Add to .env: VLA_API_KEY=<output>
```

The CI runs exactly these steps on every PR. A failing CI is a blocking issue.

---

## Engineering Contributions

### Branch naming

| Type | Prefix | Example |
|---|---|---|
| New feature | `feat/` | `feat/emergency-stop-subscriber` |
| Bug fix | `fix/` | `fix/wheel-separation-param-name` |
| Documentation | `docs/` | `docs/update-nav2-config-table` |
| Refactor | `refactor/` | `refactor/mission-planner-state-machine` |
| Tests | `test/` | `test/cmd-vel-mux-coverage` |

### Code style

- Python: formatted with **black**, linted with **ruff**. Run both before committing.
- Do not add new `# noqa` suppressions without explaining why in a comment.
- Do not comment out `ament_lint_auto` — if you add a new package, wire linting up properly.

### Parameter changes

If you change or add a ROS 2 node parameter:
1. Update the parameter table in `CLAUDE.md`.
2. Update the corresponding launch file if it passes that parameter by name.
3. The known parameter mismatch in `robot.launch.py` (`wheel_separation_x` vs `wheel_separation_length`) is a tracked bug — fix it properly, don't work around it.

### Known gaps (tracked bugs)

These are documented in `CLAUDE.md` under "Known Issues & Mismatches". When fixing one:
- Reference the issue number in your commit message.
- Do not open a new issue for something already listed there — just fix it.

### Test coverage expectations

New code must include tests. Priority areas with 0% coverage are listed in `CLAUDE.md`. See `TESTING_GUIDE.md` for patterns (mock `serial.Serial`, use `rclpy` test utilities, etc.).

---

## Research & Benchmarking Contributions

This project is working toward a paper on benchmarking VLA policies on a low-cost holonomic mobile manipulation platform. Research contributions are how you can be an author.

### What counts as a research contribution

- Running a benchmark experiment (defined below) and reporting structured results
- Collecting a demonstration dataset in the correct format
- Improving the data pipeline (`data_engine/`, `packages/robot_episode_dataset/`)
- Improving inference infrastructure (`vla_engine/`, `packages/vla_serve/`)
- Writing or improving evaluation scripts

### How to propose a benchmarking experiment

Open an issue using the **Research / Benchmark** template. Fill in:
- Model (SmolVLA, OpenVLA, or your own)
- Task description (e.g. "pick red cube from table, place in bin")
- Metrics you'll measure (success rate, inference latency, trajectory smoothness)
- Hardware config (real robot / Gazebo / both)
- How many demonstrations you plan to collect

Discuss the proposal in the issue before running the experiment. This avoids redundant experiments and keeps results comparable across contributors.

### Dataset format

All datasets must follow **LeRobot HDF5/Parquet format** as defined in `data_engine/schema/constants.py`.

Key specs:
- State: `MOBILE_MANIP_STATE_SPEC` — 9-D (arm ×6 + base ×3)
- Action: `MOBILE_MANIP_ACTION_SPEC` — 9-D (arm ×6 + base ×3)
- Cameras: `CAMERA_WRIST` (240×320, 30fps) + `CAMERA_FRONT` (480×640, 30fps)
- Sync tolerance: 50 ms

Name your dataset repo: `<your-gh-username>/omnibot-<task-slug>-<n-demos>demos`
Example: `alice/omnibot-pick-red-cube-50demos`

### Branch naming for research

Use the `bench/` prefix:
```
bench/smolvla-pick-place-50demos
bench/openvla-navigation-baseline
```

### Reporting results

Results go in your PR description using the table in the PR template. They are also tracked in issues with the `benchmarking` label so they can be pulled into the paper.

Do not fabricate or extrapolate results. See the [Code of Conduct](./CODE_OF_CONDUCT.md).

---

## Submitting a Pull Request

1. Fork the repo and create your branch from `main`.
2. Make your changes and ensure all tests pass locally.
3. Open a PR — the template will guide you through the checklist.
4. A maintainer will review within a few days. Small, focused PRs get reviewed faster.

PRs that touch `robot_ws/src/omnibot_driver/`, `vla_engine/`, or `data_engine/` require maintainer approval.

---

## Issue Labels

| Label | Meaning |
|---|---|
| `bug` | Something is broken |
| `enhancement` | New feature or improvement |
| `research` | Related to benchmarking or paper |
| `benchmarking` | Specific benchmark experiment |
| `good first issue` | Suitable for first-time contributors |
| `help wanted` | Extra attention needed |
| `documentation` | Docs-only change |

---

## Getting Help

- **GitHub Discussions**: general questions, design discussions, "how do I..." questions
- **Issues**: bug reports, feature requests, benchmark proposals only
- **Email**: Varun.vaidhiya@gmail.com for anything that shouldn't be public

If you're new and unsure where to start, look at issues tagged [`good first issue`](../../issues?q=label%3A%22good+first+issue%22). The 0%-coverage modules listed in `CLAUDE.md` are also a great place to add value quickly.
