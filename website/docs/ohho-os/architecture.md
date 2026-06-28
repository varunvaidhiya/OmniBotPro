# Architecture

OhhO OS is layered so that one API works across every robot and either runtime.
The key idea: **the robot and the middleware are swappable backends, not forks of
your code.**

```
   your code  ·  Agent (Mind)  ·  Train / Data / Serve     ← runtime-agnostic
            │
   ┌────────┴─────────┐  Robot Abstraction Layer (RAL)
   │  Runtime (port)  │  pub/sub · timers · params · capabilities
   └────────┬─────────┘
      ┌──────┴───────┐
  native runtime    ros2 runtime
  (no ROS)          (rclpy + the ROS 2 stack)
  asyncio loop      Nav2 · SLAM · MoveIt 2 · ros2_control
  direct drivers    DDS multi-machine
```

## The layers

1. **Robot Abstraction Layer (RAL).** The `Robot` API — `drive`, `move_joints`,
   `telemetry`, `emergency_stop` — plus the capability model and a unified
   command/telemetry schema. Your code talks to this and nothing below it.
2. **Runtime port.** A small interface (publish/subscribe, timers, parameters)
   that both backends implement. Engines depend on this port, never on a
   specific runtime.
3. **Backends.** The `native` runtime (a pure-Python asyncio loop with direct
   firmware / DDS / MAVLink drivers) and the `ros2` runtime (rclpy plus the full
   ROS 2 stack). See [Runtimes](runtimes.md).
4. **Adapters.** One per robot family, translating the native protocol to the
   RAL. See [Supported robots](robots.md).

## The one invariant rule

**Engines depend only on the `Runtime` port — never import a specific runtime
directly.** The agent, the learning pipelines and the perception stack are all
written against the port, so they run unchanged whether you choose native or ROS
2. This is what makes "switch runtime with one argument" possible.

## Capability-typed commands

Commands are typed by capability. A robot that lacks a capability turns the
command into a safe no-op rather than crashing, so a single behavior degrades
gracefully across heterogeneous hardware.

## Agent-native

The agent is a first-class part of the architecture, not a wrapper bolted on
top. It runs a continuous **perceive → reason → act → reflect → remember** loop,
with a pluggable brain (cloud or on-device) and tools drawn from the connected
robot's capabilities.

## Extending OhhO OS

- **Add a robot:** one manifest + one adapter + one test.
- **Add a runtime:** implement the `Runtime` port.
- **Add a skill:** register a tool the agent can call.

No platform fork, no core rewrite.
