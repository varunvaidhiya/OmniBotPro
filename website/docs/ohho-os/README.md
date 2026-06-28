# OhhO OS

**The open-source engine for any robot.** OhhO OS is the robot-agnostic runtime
that powers every OhhO product. One engine connects to anything on wheels, legs
or wings — **with or without ROS** — and carries the whole stack from perception
to training. It is yours to install, self-host and extend.

> OhhO OS is the engine; the nineteen OhhO products are consoles that run on top
> of it. Use the engine on its own, or sign in to OhhO Cloud for the managed,
> hosted experience.

## Why OhhO OS is different

- **ROS optional — never required.** Run a lightweight pure-Python runtime, or
  the full ROS 2 stack, and switch between them with a single argument.
- **Truly robot-agnostic.** Write a behavior once; run it on a wheeled base, a
  quadruped, a humanoid or an arm. Swapping hardware never means rewriting code.
- **The whole lifecycle, one engine.** Not just control — perception, data,
  training, simulation, fleet and safety, all from the same library.
- **Agent-native by design.** A continuous perceive → reason → act → reflect
  loop with a pluggable brain (cloud or on-device).
- **Training built in.** Collect demonstrations, fine-tune VLA / imitation / RL
  policies, and run continual learning from the same package.
- **Open source, zero lock-in.** MIT / Apache licensed. Bring your own models
  and data; move to the cloud only when you want to.

## Five lines to a moving robot

```python
from ohho import Robot

bot = Robot.connect("omnibot")          # auto-detects ROS or no-ROS
bot.drive(vx=0.2, vy=0.0, w=0.0)        # same API on any robot
print(bot.telemetry().odom)             # unified telemetry schema
```

## Documentation

- [Install](install.md) — pip extras, the one-line installer, requirements.
- [Quickstart](quickstart.md) — connect, drive, and run an agent in minutes.
- [Runtimes: ROS vs no-ROS](runtimes.md) — choose the runtime that fits, and
  switch later with one argument.
- [Supported robots](robots.md) — categories, the capability model, and the
  protocol adapters.
- [Training pipelines](training.md) — data collection, fine-tuning and serving.
- [Architecture](architecture.md) — the abstraction layer that makes one API
  work across every robot and runtime.

## Where it fits

OhhO OS is built on the same open engines that power the platform — the agent
harness, the learning engine, the inference server and the robot drivers — unified
behind one installable package and one consistent API.
