# Supported robots

OhhO OS is robot-agnostic by design. A **capability model** describes what a
robot can do — not how — so the same code runs across very different hardware,
and **adapters** translate each robot's native protocol underneath.

## Robot categories

OhhO OS organizes hardware into categories, each with one or more models:

- Wheeled (differential, mecanum, ackermann, omnidirectional)
- Legged (quadrupeds, hexapods)
- Humanoid
- Industrial arms
- Mobile manipulators
- Drones
- Tracked, Marine, Underwater ROV
- Delivery, Inspection, Agricultural, Medical, and more

## The capability model

Every robot ships a manifest declaring its capabilities and limits:

```yaml
id: unitree-go2
category: legged
capabilities: [base.holonomic_drive, perception.depth, perception.rgb, legged.pose]
dof: { base: 3, arm: 0 }
adapter: unitree-dds
limits: { max_lin: 1.5, max_ang: 2.0 }
```

Your code asks what a robot can do, rather than assuming:

```python
if bot.has("manipulation"):
    bot.move_joints(...)
else:
    bot.drive(...)          # graceful degradation, never a crash
```

This is what makes a behavior — or an entire agent — portable across a wheeled
base, a quadruped, a humanoid and an arm with no rewrite.

## Protocol adapters

Adapters connect the unified API to each robot's native protocol:

| Adapter | Brand / type | Transport |
|---|---|---|
| Unitree DDS | Unitree G1, Go2, H1, H1-2, H2, B2, A2 | Cyclone DDS |
| DJI MAVLink | DJI drones / MAVLink platforms | MAVLink v2 |
| Modbus | Generic PLC arms | Modbus TCP / RTU |
| EtherCAT | UR5e, UR10e, Franka Panda | EtherCAT |
| Yahboom serial | OmniBot, Rosmaster X3 | USB serial |
| ROSBridge | Any ROS 2 robot | WebSocket |
| Web Serial / BLE | Firmware-direct, browser | USB / Bluetooth |
| Simulator | No hardware | In-process |

## Add a robot

Adding a new robot is a small, contained change: **one manifest + one adapter +
one test**. No platform fork, no core rewrite — see
[Architecture](architecture.md) for the adapter interface.
