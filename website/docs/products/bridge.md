# OhhO Bridge

**Connect any robot. Even the ones that don't speak ROS.**

- **Category:** Foundation
- **Accent:** violet
- **Live app:** [Open bridge console](/bridge)

Per-brand protocol adapters that translate between native robot SDKs and the OhhO
platform. Bridge a Unitree DDS humanoid, a DJI drone or a Modbus arm into
standard ROS 2 topics — no fork, no rewrite.

## Overview (hero)

OhhO Connect handles the transport; OhhO Bridge handles the language. Bridge is a
library of per-brand protocol adapters that translate between a robot's native
SDK — Unitree's DDS LowCmd/LowState, DJI's MAVLink, a Modbus PLC arm — and the
standard ROS 2 topics every OhhO product already speaks. One adapter per brand,
and any robot joins the platform.

## Highlights

- Unitree DDS ↔ ROS 2 topics
- DJI MAVLink ↔ ROS 2 topics
- Modbus / PLC arm adapters
- Joint-index maps per robot model
- Impedance-gain defaults included

## What you get

- ROS 2 is the lingua franca of the OhhO platform — but most commercial robots
  don't speak it natively. Unitree humanoids talk Cyclone DDS through unitree_sdk2
  with custom LowCmd/LowState IDL. DJI drones speak MAVLink. Industrial arms speak
  Modbus or EtherCAT. OhhO Bridge is the layer that translates each one into the
  standard ROS 2 topics the rest of the platform expects.
- Each bridge is a thin ROS 2 node — or a browser-side codec for Web Serial —
  that subscribes to the robot's native protocol and republishes as standard
  topics: Twist on /cmd_vel, JointState on /joint_states, Imu on /imu/data,
  Odometry on /odom. In the other direction, it takes your ROS 2 commands and
  calls the robot's native SDK. For Unitree, that means mapping Twist to HighCmd
  velocity fields and JointState to LowCmd motor commands with sensible default
  impedance gains (kp/kd).
- Bridge is what makes 'any robot' literally true. Without it, OhhO's intelligence
  and operations products work on any ROS 2-compatible robot — which is a lot, but
  not everything. With Bridge, a Unitree G1 humanoid, a DJI Matrice drone and a
  Modbus-controlled SCARA arm all appear to the platform as standard ROS 2 robots,
  and every console works unchanged.

## Features

- **Unitree DDS adapter** — Translates unitree_sdk2 LowCmd/LowState and
  HighCmd/HighState to and from standard ROS 2 topics, with per-model joint-index
  maps for G1, H1, H2, Go2 and B2.
- **DJI MAVLink adapter** — Bridges MAVLink heartbeat, attitude, global position
  and manual control to ROS 2 Imu, Odometry and Twist — so a drone appears in the
  platform like any other robot.
- **Industrial arm adapters** — Modbus TCP/RTU and EtherCAT bridges for
  PLC-driven arms, exposing joint state and joint commands as standard ROS 2
  topics.
- **Impedance-gain defaults** — When translating ROS joint commands into Unitree
  LowCmd motor commands, Bridge applies sensible default kp/kd profiles per joint
  — so position control works out of the box without per-servo tuning.
- **Browser-side codecs** — For Web Serial connections, Bridge ships
  browser-native protocol codecs — like the Yahboom packet encoder — so Connect
  can talk firmware-direct with no Pi in the loop.
- **Community-extensible** — Each bridge is a standalone adapter module. New
  brands are added as a new adapter — no platform fork, no core rewrite.

## How it works

1. **Install the bridge** — Select the bridge for your robot brand — Unitree, DJI,
   or a community adapter — and install it alongside OhhO Frame.
2. **Map the joints** — Bridge loads the joint-index map for your specific model
   and applies default impedance gains.
3. **Run the node** — The bridge node connects to the robot's native SDK and
   starts republishing standard ROS 2 topics.
4. **Use every console** — Pilot, Autonomy, Fleet, Mind and every other OhhO
   product now work on your robot unchanged.

## Specs

| Spec | Value |
|---|---|
| Adapters | Unitree DDS, DJI MAVLink, Modbus, EtherCAT, Yahboom serial |
| Unitree models | G1, H1, H1-2, H2, R1, Go2, B2, A2 |
| ROS 2 topics | /cmd_vel, /joint_states, /imu/data, /odom |
| Gain profiles | Default kp/kd per joint per model |
| Runtime | ROS 2 node (Pi / onboard PC) + browser codecs |
| Integrates | OhhO Connect, Frame, all consoles |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | 1 brand adapter | ✅ |
| Fleet | All brand adapters | ✅ |
| Forge | Custom protocol adapters | ✅ |

**Recommended plan: Builder.** Most teams only need one bridge — the one for
their robot. Builder includes a single brand adapter, which is enough to bring one
robot family onto the platform. Teams running mixed fleets choose Fleet for all
adapters; enterprises with proprietary protocols choose Forge for custom bridges.

## FAQ

**Do I need Bridge if my robot already speaks ROS 2?**
No. If your robot publishes standard ROS 2 topics natively, Connect + Frame are
enough. Bridge is for robots that speak a native non-ROS protocol — Unitree DDS,
DJI MAVLink, Modbus, and so on.

**Which Unitree models are supported?**
The Unitree DDS bridge covers G1, H1, H1-2, H2, R1, Go2, B2 and A2, using the
joint-index maps from Unitree's published URDF and SDK headers.

**Can I write my own bridge?**
Yes. Each bridge is a standalone adapter module. On Forge, the OhhO team builds
and maintains custom bridges for proprietary protocols.

## Related products

- [OhhO Connect](./connect.md)
- [OhhO Frame](./frame.md)
- [OhhO Pilot](./pilot.md)
