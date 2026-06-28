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

## Built-in robots (v1.0.0)

Three robots ship in the box:

| Robot ID | Name | Category | Capabilities | Adapter |
|---|---|---|---|---|
| `omnibot` | OmniBot | mobile-manipulator | base.drive, base.holonomic_drive, manipulation, perception.rgb, perception.depth | yahboom-serial + feetech (composite) |
| `unitree-go2` | Unitree Go2 | legged | base.drive, base.holonomic_drive, legged.pose, perception.rgb, perception.depth | unitree-dds |
| `sim` | Generic Sim Bot | wheeled | base.drive, base.holonomic_drive, perception.rgb | sim |

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

### Capability vocabulary

| Capability | Description |
|---|---|
| `base.drive` | The robot can move linearly and turn |
| `base.holonomic_drive` | The robot can strafe (linear_y) — implies base.drive |
| `legged.pose` | The robot is legged and can pose |
| `manipulation` | The robot has an arm with joints |
| `perception.rgb` | The robot has an RGB camera |
| `perception.depth` | The robot has a depth camera |

## Protocol adapters (v1.0.0)

Six adapters ship in v1.0.0:

| Adapter | Scheme | Brand / type | Transport | Extra |
|---|---|---|---|---|
| `sim` | `sim://` | Built-in simulator | In-process (holonomic physics) | none |
| `yahboom` | `serial://` | Yahboom Rosmaster X3 / OmniBot base | USB serial @ 115200 baud | `[serial]` |
| `feetech` | `feetech://` | SO-101 6-DOF arm (STS3215 servos) | USB serial @ 1 Mbps | `[arm]` |
| `composite` | `serial://base,arm` | Merges a base + arm transport | Delegates to both | inherits |
| `unitree` | `dds://` | Unitree G1, Go2, H1, B2, A2 | Cyclone DDS | `[unitree]` |
| `ros2` | `ros2://` | Any ROS 2 robot | ROS 2 topics (DDS) | `[ros2]` |

### Composite transport

Mobile-manipulators like OmniBot have two independent physical links — a base
(Yahboom serial) and an arm (Feetech serial). The composite transport merges
them into one `Transport`:

- `send_velocity` → base (the arm has no wheels)
- `send_joint_command` → arm (the base board doesn't drive the arm)
- `read` / telemetry → merged (odom from base, joints from arm)
- `emergency_stop` / `release_stop` → both

```python
# Auto-composes a composite for a robot with manipulation capability:
bot = Robot.connect("omnibot", "serial:///dev/ttyUSB0,/dev/ttyACM0")
```

## Add a robot

Adding a new robot is a small, contained change: **one manifest + one adapter +
one test**. No platform fork, no core rewrite.

### 1. Write a manifest (JSON or YAML)

```json
{
  "id": "my-bot",
  "name": "My Bot",
  "category": "wheeled",
  "capabilities": ["base.drive", "perception.rgb"],
  "adapter": "my-adapter",
  "dof_base": 3,
  "max_lin": 0.5,
  "max_ang": 1.0
}
```

### 2. Write an adapter (subclass `BaseTransport`)

```python
from ohho.transport import BaseTransport

class MyAdapterTransport(BaseTransport):
    protocol = "my-adapter"
    # implement connect, send_velocity, read, send_joint_command, etc.
```

### 3. Register the scheme + write a test

See [Contributing](https://github.com/varunvaidhiya/OmniBotPro/blob/main/sdk/CONTRIBUTING.md)
for the full guide.
