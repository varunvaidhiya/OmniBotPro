# Quickstart

Connect to a robot, drive it, and hand it a goal — in a few lines. The same code
works on any robot and either runtime.

> **New here?** Start with the [Setup Guide](setup.md) for a complete walkthrough
> with illustrations.

## 1. Connect

```python
from ohho import Robot

# Auto-detect the best runtime, or name a transport explicitly.
bot = Robot.connect("omnibot")                       # auto — sim fallback
dog = Robot.connect("unitree-go2", "dds://192.168.1.10")
arm_bot = Robot.connect("omnibot", "serial:///dev/ttyUSB0,/dev/ttyACM0")
ros_bot = Robot.connect("omnibot", "ros2://", runtime="ros2")
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
bot.release_stop()                    # resume
```

## 3. Hand it a goal (the agent)

```python
from ohho.agent import Agent

agent = Agent(bot)                    # pluggable brain: cloud or on-device
log = agent.run("find the red cup and bring it to the kitchen")
for line in log:
    print(line)
```

The agent runs a continuous **perceive → reason → act → reflect** loop. With
the `[agent]` extra + `ANTHROPIC_API_KEY`, it uses a Claude tool-calling
reasoner that builds tools from the robot's capabilities. Without it, a
`ScriptedBrain` fallback exercises the robot deterministically.

## 4. Record data and train

```python
from ohho.data import Recorder

rec = Recorder(bot, repo_id="local/demo", fps=10.0)
rec.start_episode(task="pick up the cup")
for _ in range(50):
    bot.drive(vx=0.1)
    rec.capture_frame()
rec.stop_episode()
rec.save("~/datasets/demo")
```

```python
from ohho.train import finetune

ckpt = finetune(dataset="~/datasets/demo", policy="smolvla", device="auto")
```

## 5. Serve the trained policy

```bash
ohho serve --checkpoint ./checkpoints/smolvla --port 8000
```

## 6. Run a skill

```bash
ohho market list
ohho market run omnibot patrol
```

## CLI shortcuts

```bash
ohho doctor                 # check your environment
ohho list                   # list built-in robots
ohho connect omnibot        # connect + print status + telemetry
ohho sim --robot omnibot    # launch the built-in simulator
ohho drive omnibot --vx 0.15 # drive forward
ohho agent omnibot "tidy the desk"  # hand the agent a goal
ohho serve --port 8000      # serve a policy over REST
ohho market list            # list registered skills
ohho profile detect         # detect your hardware profile
```

## The same loop on a different robot

Because behavior is written against capabilities, not a specific robot, the exact
agent code above runs unchanged on the quadruped or the arm:

```python
Agent(dog).run("patrol the warehouse and report obstacles")
```

## Next

- [Setup Guide](setup.md) — complete walkthrough with illustrations
- [Runtimes: ROS vs no-ROS](runtimes.md)
- [Supported robots](robots.md)
- [Training pipelines](training.md)
- [Skills & Market](skills.md)
