# OhhO OS

**The open-source engine for any robot.** OhhO OS is the robot-agnostic runtime
that powers every OhhO product. One engine connects to anything on wheels, legs
or wings — **with or without ROS** — and carries the whole stack from perception
to training. It is yours to install, self-host and extend.

> OhhO OS is the engine; the nineteen OhhO products are consoles that run on top
> of it. Use the engine on its own, or sign in to OhhO Cloud for the managed,
> hosted experience.

![OhhO OS v1.0.0 Feature Map](/docs/ohho-os/features.svg)

## Why OhhO OS is different

- **ROS optional — never required.** Run a lightweight pure-Python runtime, or
  the full ROS 2 stack, and switch between them with a single argument.
  `get_runtime("auto")` detects ROS 2 automatically.
- **Truly robot-agnostic.** Write a behavior once; run it on a wheeled base, a
  quadruped, a humanoid or an arm. Swapping hardware never means rewriting code.
  A capability model gates commands — a robot that can't do something turns the
  call into a safe no-op.
- **The whole lifecycle, one engine.** Not just control — agent, data
  collection, training, serving, and skills, all from the same `pip install`.
- **Agent-native by design.** A continuous perceive → reason → act → reflect
  loop powered by `agent_engine` with an LLM tool-calling reasoner. Tools are
  built automatically from the robot's capabilities.
- **Training built in.** Record demonstrations → fine-tune VLA policies
  (SmolVLA, ACT, diffusion, OpenVLA) → serve over REST → close the loop. All
  with `device="auto"` hardware detection.
- **Open source, zero lock-in.** Apache-2.0 licensed. Bring your own models
  and data; move to the cloud only when you want to.

## Five lines to a moving robot

```python
from ohho import Robot

bot = Robot.connect("omnibot")          # auto-detects ROS or no-ROS
bot.drive(vx=0.2, vy=0.0, w=0.0)        # same API on any robot
print(bot.telemetry().odom)             # unified telemetry schema
```

## What's in v1.0.0

| Area | What ships |
|---|---|
| **Control** | Robot Abstraction Layer, 6 adapters (sim, serial base, bus-servo arm, DDS-native, ROS 2, composite), 2 runtimes (native + ROS 2), capability-gated commands |
| **Agent** | HarnessBrain → agent_engine (perceive→reason→act→reflect), ClaudeToolCallingReasoner, ToolRegistry from capabilities, ScriptedBrain fallback |
| **Data** | Recorder (intercepts drive/move_joints → 9-D state+action), LeRobot v2.0 writer (Parquet + meta JSON), DatasetReader |
| **Training** | `finetune()` delegates to lerobot_engine (smolvla/act/diffusion/openvla), mock mode for sim loops, `device="auto"` |
| **Serving** | FastAPI server (`/health`, `/load_model`, `/predict`), `ohho serve` CLI, mock model for sim |
| **Skills** | `@skill` decorator, `ohho market list/run`, 4 built-in skills (patrol, wave, stop, status) |
| **Profiles** | 5 hardware profiles (pi_workstation, jetson_single, workstation_single, mac_dev, edge_cpu), `ohho profile detect` |
| **CLI** | `ohho doctor list version connect sim drive agent serve market profile` |
| **Tests** | 151 unittest cases + 4 HIL tests (skip unless `OHHO_HIL=1`) |

## Documentation

- [Setup Guide](setup.md) — **start here** — a complete walkthrough from install
  to your first moving robot, with illustrations.
- [Install](install.md) — pip extras, requirements, verification.
- [Quickstart](quickstart.md) — connect, drive, and run an agent in minutes.
- [Runtimes: ROS vs no-ROS](runtimes.md) — choose the runtime that fits, and
  switch later with one argument.
- [Supported robots](robots.md) — categories, the capability model, and the
  6 protocol adapters shipped in v1.0.0.
- [Native navigation](navigation.md) — occupancy-grid mapping, A* planning,
  frontier exploration — no ROS required.
- [Memory & perception](memory.md) — spatio-temporal memory (object permanence,
  temporal queries) + sim and vision perceptors.
- [Training pipelines](training.md) — data collection, fine-tuning and serving.
- [Architecture](architecture.md) — the abstraction layer that makes one API
  work across every robot and runtime.
- [Skills & Market](skills.md) — the skill registry, built-in skills, and how to
  write your own.

## Where it fits

OhhO OS is built on the same open engines that power the platform — the agent
harness, the learning engine, the inference server and the robot drivers — unified
behind one installable package and one consistent API.
