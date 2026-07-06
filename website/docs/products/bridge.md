# OhhO Bridge

**Connect any robot. Even the ones that don't speak ROS.**

- **Category:** Foundation
- **Accent:** violet
- **Live app:** [Open bridge console](/bridge)

Protocol adapters that translate between any robot's native protocol and the OhhO
platform. Bridge a DDS-native humanoid, a MAVLink drone, a CANopen mobile base,
an OPC UA factory cell or a Modbus arm into standard ROS 2 topics — no fork, no
rewrite.

## Overview (hero)

OhhO Connect handles the transport; OhhO Bridge handles the language. Bridge is a
library of protocol adapters that translate between a robot's native protocol — a
DDS LowCmd/LowState interface, a MAVLink autopilot, a CANopen motor bus, an OPC UA
factory cell, a Modbus PLC arm — and the standard ROS 2 topics every OhhO product
already speaks. One adapter per protocol family, and any robot joins the platform.

## Highlights

- DDS-native ↔ ROS 2 topics
- MAVLink ↔ ROS 2 topics
- CAN bus / CANopen (CiA 402) ↔ ROS 2
- OPC UA ↔ ROS 2 (Industry 4.0)
- PROFINET · EtherNet/IP ↔ ROS 2
- MQTT · VDA 5050 (AGV/AMR fleets)
- Modbus · EtherCAT (industrial arms)
- ROS-Industrial arm driver compatibility
- Joint-index maps per robot model
- Impedance-gain defaults included

## What you get

- ROS 2 is the lingua franca of the OhhO platform — but most commercial robots
  don't speak it natively. Humanoids talk Cyclone DDS through a native SDK with
  custom LowCmd/LowState IDL. Drones speak MAVLink. Mobile bases and AGVs speak
  CANopen over a CAN bus. Factory cells speak OPC UA. Industrial arms speak
  Modbus, EtherCAT, PROFINET or EtherNet/IP. Warehouse fleets speak VDA 5050 over
  MQTT. OhhO Bridge is the layer that translates each one into the standard ROS 2
  topics the rest of the platform expects.
- Each bridge is a thin ROS 2 node — or a browser-side codec for Web Serial —
  that subscribes to the robot's native protocol and republishes as standard
  topics: Twist on /cmd_vel, JointState on /joint_states, Imu on /imu/data,
  Odometry on /odom. In the other direction, it takes your ROS 2 commands and
  calls the robot's native protocol. For a DDS-native humanoid, that means
  mapping Twist to HighCmd velocity fields and JointState to LowCmd motor
  commands with sensible default impedance gains (kp/kd). For a CANopen base, it
  means SDO/PDO object dictionary translation. For an OPC UA cell, it means
  browsing the server's address space and mapping nodes to topics.
- Bridge is what makes 'any robot' literally true. Without it, OhhO's
  intelligence and operations products work on any ROS 2-compatible robot — which
  is a lot, but not everything. With Bridge, a DDS-native humanoid, a MAVLink
  survey drone, a CANopen AGV, an OPC UA-integrated factory arm and a
  Modbus-controlled SCARA all appear to the platform as standard ROS 2 robots,
  and every console works unchanged. And because Bridge speaks the industry
  standards the factory floor already runs on — PROFINET for German automotive,
  EtherNet/IP for North American manufacturing, VDA 5050 for warehouse fleets —
  a robot on OhhO plugs into the systems your facility already has, not the other
  way around.

## Features

- **DDS-native humanoid adapter** — Translates a native DDS LowCmd/LowState and
  HighCmd/HighState interface to and from standard ROS 2 topics, with per-model
  joint-index maps for the humanoid family you're driving.
- **MAVLink drone adapter** — Bridges MAVLink heartbeat, attitude, global
  position and manual control to ROS 2 Imu, Odometry and Twist — so a drone
  appears in the platform like any other robot.
- **CAN bus / CANopen adapter** — Translates CANopen object dictionaries
  (CiA 402 motion profile, SDO/PDO) to ROS 2 JointState, Twist and Odometry —
  the standard protocol for mobile robot motor controllers, AGVs and embedded
  bases.
- **OPC UA adapter** — Browses an OPC UA server's address space and maps nodes
  to ROS 2 topics — so a robot integrates with Industry 4.0 factory cells,
  MES/SCADA systems and digital twin platforms that already speak OPC UA.
- **PROFINET & EtherNet/IP adapters** — Real-time industrial Ethernet bridges
  for the two dominant factory-network ecosystems — PROFINET for European/Siemens
  manufacturing, EtherNet/IP for North American/Rockwell manufacturing. A robot
  on OhhO speaks the network your plant already runs.
- **MQTT & VDA 5050 adapter** — Bridges the VDA 5050 AGV/AMR fleet standard over
  MQTT — so OhhO Fleet interoperates with warehouse management systems and master
  control software from Linde, Toyota, MiR, KION and the rest of the VDA 5050
  ecosystem.
- **Industrial arm adapters** — Modbus TCP/RTU and EtherCAT bridges for
  PLC-driven arms, plus ROS-Industrial compatibility for the major industrial
  arm families — exposing joint state and joint commands as standard ROS 2
  topics.
- **PLC integration (IEC 61131-3)** — Bridge maps ROS 2 topics to PLC-readable
  signals, so a robot exchanges data with controllers programmed in ladder logic,
  structured text or function blocks — the languages every factory PLC already
  speaks.
- **Impedance-gain defaults** — When translating ROS joint commands into a
  native DDS motor command, Bridge applies sensible default kp/kd profiles per
  joint — so position control works out of the box without per-servo tuning.
- **Browser-side codecs** — For Web Serial connections, Bridge ships
  browser-native protocol codecs for common serial bases — so Connect can talk
  firmware-direct with no onboard PC in the loop.
- **Community-extensible** — Each bridge is a standalone adapter module. New
  protocols are added as a new adapter — no platform fork, no core rewrite.

## How it works

1. **Install the bridge** — Select the bridge for your robot's protocol — DDS,
   MAVLink, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, Modbus,
   serial, or a community adapter — and install it alongside OhhO Frame.
2. **Map the joints / signals** — Bridge loads the joint-index map for a
   humanoid, the object dictionary for a CANopen base, or the OPC UA address
   space for a factory cell — and applies sensible defaults.
3. **Run the node** — The bridge node connects to the robot's native protocol
   and starts republishing standard ROS 2 topics.
4. **Use every console** — Pilot, Autonomy, Fleet, Mind and every other OhhO
   product now work on your robot unchanged — and your plant's existing systems
   keep speaking their own protocols.

## Supported protocols

| Protocol | Category | What it unlocks | Typical use |
|---|---|---|---|
| DDS (Cyclone) | Robot | Native humanoid/quadruped SDK translation | Humanoids, legged robots |
| MAVLink | Robot | Drone autopilot bridge | Aerial drones, survey, inspection |
| CANopen (CiA 402) | Robot | Motor controller bus translation | AGVs, AMRs, embedded mobile bases |
| Modbus TCP/RTU | Robot | PLC-driven arm joint control | Industrial SCARA / 6-DOF arms |
| EtherCAT | Robot | Real-time arm servo control | High-performance industrial arms |
| Serial (vendor) | Robot | Firmware-direct base control | Hobby/research bases (Web Serial) |
| OPC UA | Factory | Industry 4.0 cell & MES/SCADA integration | Factory floor, digital twins |
| PROFINET | Factory | Siemens / European manufacturing network | German automotive, European plants |
| EtherNet/IP (CIP) | Factory | Rockwell / North American manufacturing | US factories, automotive |
| IEC 61131-3 | Factory | PLC signal exchange | Factory PLC controllers |
| MQTT | Fleet | IoT messaging, cloud telemetry | Cloud fleet, IoT platforms |
| VDA 5050 | Fleet | AGV/AMR fleet standard (over MQTT) | Warehouse robotics, WMS integration |
| ROS-Industrial | Arms | Fanuc, ABB, KUKA, Yaskawa, UR drivers | Industrial arm deployment |

## Specs

| Spec | Value |
|---|---|
| Robot protocols | DDS, MAVLink, CANopen, Modbus, EtherCAT |
| Factory protocols | OPC UA, PROFINET, EtherNet/IP, IEC 61131-3 |
| Fleet protocols | MQTT, VDA 5050 |
| Industrial arms | ROS-Industrial compatible, Modbus, EtherCAT |
| Humanoid models | Per-model joint maps, configurable for your robot |
| ROS 2 topics | /cmd_vel, /joint_states, /imu/data, /odom |
| Gain profiles | Default kp/kd per joint per model |
| Runtime | ROS 2 node (onboard PC) + browser codecs |
| Integrates | OhhO Connect, Frame, all consoles |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | 1 brand adapter | ✅ |
| Fleet | All brand adapters | ✅ |
| Forge | Custom protocol adapters | ✅ |

**Recommended plan: Builder.** Most teams only need one bridge — the one for
their robot. Builder includes a single adapter, which is enough to bring one
robot family onto the platform. Teams running mixed fleets choose Fleet for all
adapters; enterprises with proprietary protocols choose Forge for custom bridges.

## FAQ

**Do I need Bridge if my robot already speaks ROS 2?**
No. If your robot publishes standard ROS 2 topics natively, Connect + Frame are
enough. Bridge is for robots that speak a native non-ROS protocol — DDS, MAVLink,
CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, Modbus, and so on.

**Which humanoid models are supported?**
The DDS bridge covers any humanoid with a published URDF and SDK header — Bridge
builds the joint-index map from those, so a new model is one config away.

**Can Bridge integrate with my factory's PLC and SCADA systems?**
Yes. The OPC UA adapter browses your server's address space and maps nodes to
ROS 2 topics; the PROFINET and EtherNet/IP adapters speak the industrial Ethernet
your plant already runs; and IEC 61131-3 mapping lets PLCs exchange signals with
robots over standard topics.

**Does Bridge support warehouse AGV/AMR fleet standards?**
Yes. The VDA 5050 adapter over MQTT lets OhhO Fleet interoperate with warehouse
management systems and master control software that speak the VDA 5050 standard —
so your robots integrate with the fleet infrastructure your warehouse already has.

**Can I write my own bridge?**
Yes. Each bridge is a standalone adapter module. On Forge, the OhhO team builds
and maintains custom bridges for proprietary protocols.

## Related products

- [OhhO Connect](./connect.md)
- [OhhO Frame](./frame.md)
- [OhhO Pilot](./pilot.md)
