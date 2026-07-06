# Runtimes: ROS vs no-ROS

OhhO OS ships **two runtimes behind one identical API**. ROS-ness is a backend
choice, not a rewrite of your code — choose what fits, and switch later by
changing a single argument.

```python
Robot.connect("omnibot", runtime="native")   # no ROS — pip and go
Robot.connect("omnibot", runtime="ros2")      # full ROS 2 stack
Robot.connect("omnibot")                        # auto-detect (prefers ros2)
```

`get_runtime("auto")` prefers `ros2` when rclpy is available, and falls back to
`native` otherwise — so the same code runs on both backends with zero changes.

## At a glance

| Capability | No-ROS (native) | ROS 2 |
|---|---|---|
| Install | `pip install` — any OS | Ubuntu 24.04 + ROS 2 Jazzy |
| Drive · teleop · telemetry | ✅ | ✅ |
| Agent (Mind) · Train · Data · Serve | ✅ | ✅ |
| Direct DDS / MAVLink / firmware | ✅ native | via bridge |
| Lightweight nav (built-in A*) | ✅ | ✅ |
| Nav2 full navigation | ❌ | ✅ |
| SLAM (slam_toolbox / 3-D) | basic | ✅ |
| MoveIt 2 manipulation | ❌ | ✅ |
| Multi-machine distribution | partial | ✅ (DDS) |
| Tooling | Foxglove | RViz + Foxglove + ecosystem |
| Footprint / cold start | light / fast | heavy |
| OS support | Windows · macOS · Linux | Linux |

Note that the intelligence rows — agent, training, data, serving — are identical
on both runtimes. Only the robotics middleware differs.

## The native runtime

`NativeRuntime` is a pure-Python threaded scheduler. It provides:

- **Timers** — a background thread fires registered timers at their period.
- **Pub/sub** — an in-process message bus (`subscribe`/`publish`).
- **Parameters** — an in-memory key-value store (`get_param`/`set_param`).

No ROS, no DDS, no heavy dependencies. Runs anywhere Python runs.

## The ROS 2 runtime

`Ros2Runtime` wraps an rclpy node. It provides:

- **Timers** — via `node.create_timer(period, callback)`.
- **Pub/sub** — via ROS 2 topics. The generic bus uses `std_msgs/String` with
  JSON-encoded payloads. `Ros2Transport` uses real message types (`Twist`,
  `Odometry`, `JointState`) for hardware topics.
- **Parameters** — via `node.declare_parameter` / `node.get_parameter`.
- **Spinning** — `start()` spins the node in a background thread; `stop()`
  cleans up all timers, publishers, subscriptions, and shuts down rclpy.

### ROS 2 transport

`Ros2Transport` bridges the unified Transport interface to real ROS 2 topics:

| OhhO | ROS 2 topic | Message type |
|---|---|---|
| `send_velocity()` | `/cmd_vel` | `geometry_msgs/Twist` |
| `read()` odom | `/odom` | `nav_msgs/Odometry` |
| `read()` joints | `/arm/joint_states` | `sensor_msgs/JointState` |
| `send_joint_command()` | `/arm/joint_commands` | `sensor_msgs/JointState` |

Namespace prefixes supported: `ros2:///robot1` → `/robot1/cmd_vel`, etc.

### Auto-detect

```python
# On a ROS 2 machine: auto picks ros2
bot = Robot.connect("omnibot", "ros2://")  # full ROS 2 stack

# On a laptop: auto picks native
bot = Robot.connect("omnibot")  # sim or direct driver
```

## Choose no-ROS if…

- You are getting started, or running a single robot
- Your hardware is non-ROS (DDS-native, MAVLink, hobby bases) and you want to
  talk to it directly
- You are on a laptop, a Jetson, or the edge, and want a small footprint
- You are on Windows or macOS

**Advantage over ROS:** zero setup friction, lightweight, pure Python, runs
anywhere.

## Choose ROS 2 if...

- You need Nav2, SLAM or MoveIt 2
- You are running multiple robots, or multiple machines
- You are integrating with an existing ROS fleet, or Gazebo / Isaac simulation
- You are going to production autonomy

**Advantage over no-ROS:** the entire mature robotics ecosystem and the most
battle-tested autonomy stacks.

## There is no wrong choice

Start native today and graduate to ROS 2 when you need it. Because both runtimes
sit behind the same abstraction, switching is one argument — your agent, training
and application code do not change:

```python
# Monday: prototype with no dependencies
bot = Robot.connect("omnibot", runtime="native")

# Later: same code, now on the full ROS 2 stack
bot = Robot.connect("omnibot", runtime="ros2")
```

## How it works

See [Architecture](architecture.md) for the `Runtime` port both backends
implement, and the rule that keeps every engine independent of ROS.
