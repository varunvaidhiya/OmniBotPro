# Quickstart

Connect to a robot, drive it, and hand it a goal — in a few lines. The same code
works on any robot and either runtime.

## 1. Connect

```python
from ohho import Robot

# Auto-detect the best runtime, or name a transport explicitly.
bot = Robot.connect("omnibot")                       # auto
dog = Robot.connect("unitree-go2", "dds://192.168.1.10")
arm = Robot.connect("ur5e", runtime="ros2")
```

No hardware? Use the simulator — always available, no setup:

```python
bot = Robot.connect("omnibot", transport="sim://")
```

## 2. Drive and read telemetry

Every robot exposes the same surface; capabilities a robot lacks are no-ops.

```python
bot.drive(vx=0.2, vy=0.0, w=0.3)     # holonomic where supported
print(bot.telemetry().odom)          # x, y, theta, vx, vy, omega

if bot.has("manipulation"):
    bot.move_joints([0.0, -0.5, 0.5, 0.0, 0.0, 0.2])

bot.emergency_stop()                  # halt everything immediately
```

## 3. Hand it a goal (the agent)

```python
from ohho.agent import Agent

agent = Agent(bot)                    # pluggable brain: cloud or on-device
agent.run("find the red cup and bring it to the kitchen")
```

The agent runs a continuous **perceive → reason → act → reflect** loop, using the
robot's perception and the tools its capabilities expose.

## 4. The same loop on a different robot

Because behavior is written against capabilities, not a specific robot, the exact
agent code above runs unchanged on the quadruped or the arm:

```python
Agent(dog).run("patrol the warehouse and report obstacles")
```

## CLI shortcuts

```bash
ohho connect omnibot          # interactive connect + live telemetry
ohho drive omnibot            # keyboard teleop
ohho agent omnibot "tidy the desk"
ohho sim                      # launch the built-in simulator
```

## Next

- [Runtimes: ROS vs no-ROS](runtimes.md)
- [Supported robots](robots.md)
- [Training pipelines](training.md)
