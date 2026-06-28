# Architecture

OhhO OS is layered so that one API works across every robot and either runtime.
The key idea: **the robot and the middleware are swappable backends, not forks of
your code.**

![Architecture Diagram](/docs/ohho-os/architecture.svg)

## The layers

1. **Robot Abstraction Layer (RAL).** The `Robot` API — `drive`, `move_joints`,
   `telemetry`, `emergency_stop`, `has` — plus the capability model and a unified
   command/telemetry schema. Your code talks to this and nothing below it.

2. **Runtime port.** A small interface (publish/subscribe, timers, parameters)
   that both backends implement. Engines depend on this port, never on a
   specific runtime.

3. **Runtimes.** The `native` runtime (a pure-Python threaded scheduler with
   direct firmware / DDS / MAVLink drivers) and the `ros2` runtime (rclpy plus
   the full ROS 2 stack — Nav2, SLAM, MoveIt 2, ros2_control). See
   [Runtimes](runtimes.md).

4. **Adapters.** One per robot family, translating the native protocol to the
   RAL. v1.0.0 ships 6 adapters: `sim`, `yahboom` (serial), `feetech` (arm),
   `unitree` (DDS), `ros2` (ROS 2 topics), and `composite` (base + arm merge).

5. **Engines.** The agent (`ohho.brains.HarnessBrain`), data collection
   (`ohho.data.Recorder`), training (`ohho.train.finetune`), and serving
   (`ohho.serve`) modules. All depend only on the `Runtime` port and the
   `Transport` interface.

## The one invariant rule

**Engines depend only on the `Runtime` port and the `Transport` interface —
never import `rclpy`, `serial`, `lerobot`, or a specific adapter directly.** This
is what makes "switch runtime / robot with one argument" true. New backends and
adapters implement the interface; nothing above them changes.

## Capability-typed commands

Commands are typed by capability. A robot that lacks a capability turns the
command into a safe no-op rather than crashing, so a single behavior degrades
gracefully across heterogeneous hardware.

```python
if bot.has("manipulation"):
    bot.move_joints([0, -0.5, 0.5, 0, 0, 0.2])  # runs on OmniBot
else:
    bot.drive(vx=0.1)                             # graceful on Go2 (no arm)
```

The capability vocabulary: `base.drive`, `base.holonomic_drive`, `legged.pose`,
`manipulation`, `perception.rgb`, `perception.depth`.

## Agent-native

The agent is a first-class part of the architecture, not a wrapper bolted on
top. `HarnessBrain` wires `agent_engine.AgentHarness` — a continuous
**perceive → reason → act → reflect → remember** loop with:

- A `ToolRegistry` built automatically from the robot's capabilities (`drive`,
  `move_joints`, `stop`, `emergency_stop`, `get_telemetry`, `get_status`).
- A `RobotPerceptor` that turns `Robot.telemetry()` into a `WorldState`.
- A `ClaudeToolCallingReasoner` over a `ReasoningRouter` (cloud Claude when
  `ANTHROPIC_API_KEY` + `anthropic` are present, echo fallback otherwise).
- `ScriptedBrain` as the no-dependency fallback when `agent_engine` isn't
  installed.

## The record → train → serve loop

![Training Loop](/docs/ohho-os/training-loop.svg)

- **Record**: `Recorder` wraps a `Robot`, intercepts `drive()` and
  `move_joints()` to capture actions, polls `telemetry()` for state. Writes
  LeRobot v2.0 datasets (Parquet + meta JSON).
- **Train**: `finetune()` delegates to `lerobot_engine` (torch + LeRobot).
  Supports smolvla, act, diffusion, openvla. `mock=True` for sim loops.
- **Serve**: FastAPI server with `/health`, `/load_model`, `/predict`.
  `ohho serve` CLI. Mock model for sim.

See [Training pipelines](training.md) for the full guide.

## Extending OhhO OS

- **Add a robot:** one manifest + one adapter + one test. See
  [Contributing](https://github.com/varunvaidhiya/OmniBotPro/blob/main/sdk/CONTRIBUTING.md).
- **Add a runtime:** implement the `Runtime` port.
- **Add a skill:** `@skill` decorator + `register_skill()`. See
  [Skills](skills.md).

No platform fork, no core rewrite.
