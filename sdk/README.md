# OhhO OS

**The open-source engine for any robot.** One robot-agnostic runtime that
controls anything on wheels, legs or wings — **with or without ROS** — and
carries the whole stack from perception to training.

> This is the engine the [OhhO platform](https://ohho-robotics.com) products run
> on. It is installable and usable on its own.

## Status

`v1.1.0` — **M0–M5 complete + native nav & memory.** Highlights: 6 adapters
(sim, Yahboom serial, Feetech arm, composite, Unitree DDS, ROS 2 topics), two
runtimes (native + ROS 2, auto-detected), a real agent brain
(perceive→reason→act→reflect with tools built from capabilities), the
record→train→serve pipeline, the skill market, hardware profiles, **built-in
navigation** (occupancy mapping, A*, frontier exploration — `ohho nav`), and
**spatio-temporal memory + perception** (`ohho look` / `ohho memory`,
Claude-vision optional). See `AGENTS.md` for the full state and
`website/docs/ohho-os/` for user docs.

The original M0 scaffold, for reference:

- The Robot Abstraction Layer (`Robot`) with a capability model
- A `native` runtime (no ROS) and a `ros2` runtime stub behind one `Runtime` port
- A deterministic in-process **simulator** adapter (no hardware required)
- A robot registry with built-in robots (OmniBot, Unitree Go2, a generic sim bot)
- The `ohho` CLI (`doctor`, `sim`, `connect`, `drive`, `agent`, `list`)

Real hardware adapters (Unitree DDS, DJI MAVLink, …), the full ROS 2 backend, and
the training pipelines are wired as the next milestones — they plug into the same
abstraction this scaffold defines.

## Install

```bash
pip install -e 'sdk[base]'        # from the repo root (editable)
# extras: [unitree] [dji] [ros2] [train] [serve] [yaml] [all]
```

## Quickstart

```python
from ohho import Robot

bot = Robot.connect("omnibot")        # runs in simulation until a real adapter is installed
bot.drive(vx=0.2, w=0.3)
print(bot.telemetry().odom)
if bot.has("manipulation"):
    bot.move_joints([0, -0.5, 0.5, 0, 0, 0.2])
bot.disconnect()
```

## CLI

```bash
ohho doctor                 # environment, runtimes, adapters, robots
ohho list                   # built-in robots
ohho sim --robot omnibot    # drive a pattern in simulation, stream telemetry
ohho drive omnibot --vx 0.15 --seconds 3
ohho agent omnibot "explore the room"
```

## Layout

```
ohho/
  schema.py        # Velocity, Odometry, JointReading, Telemetry, TransportStatus
  capabilities.py  # capability vocabulary
  registry.py      # RobotSpec + built-in robots + manifest loading
  transport.py     # Transport interface (ported from the web Connect layer)
  runtime/         # Runtime port + native / ros2 backends
  adapters/        # sim adapter (+ resolution); hardware adapters land here
  robot.py         # the Robot Abstraction Layer (public API)
  agent.py         # minimal agent loop (delegates to agent_engine when present)
  cli.py           # the `ohho` command
```

See `docs/ohho-os` on the website for the full design.
