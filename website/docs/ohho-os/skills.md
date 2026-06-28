# Skills & Market

Skills are reusable, named behaviors that run on any robot. They register with
a decorator and are discovered via the `ohho market` CLI. A skill's tools are
gated by the robot's capabilities — a skill that needs `manipulation` won't run
on a robot without an arm.

## Built-in skills

| Skill | Requires | Description |
|---|---|---|
| `patrol` | `base.drive` | Drive a square patrol pattern |
| `wave` | `manipulation` | Wave the arm gripper |
| `stop` | — | Emergency stop the robot |
| `status` | — | Print telemetry and connection status |

## List and run skills

```bash
ohho market list
# patrol          v0.1.0  [base.drive]   tags: navigation, demo
#   Drive a square patrol pattern. Args: side (m), speed (m/s).
# wave            v0.1.0  [manipulation]  tags: arm, demo
#   Wave the arm gripper. Args: reps (count).
# stop            v0.1.0  []              tags: safety
#   Emergency stop the robot.
# status          v0.1.0  []              tags: diagnostics
#   Print the robot's telemetry and connection status.

ohho market run omnibot patrol
# skill 'patrol': patrol complete

ohho market run omnibot wave
# skill 'wave': waved 3 times
```

## Use skills in Python

```python
from ohho.market import run_skill, list_skills, get_skill

# List all registered skills
for s in list_skills():
    print(f"{s.name}: {s.description}")

# Run a skill
result = run_skill("patrol", bot, side=2.0, speed=0.15)

# Check if a skill can run on a robot
skill = get_skill("wave")
if skill.can_run(bot):
    skill.run(bot, reps=5)
```

## Write your own skill

Use the `@skill` decorator to register a reusable behavior:

```python
from ohho.market import skill

@skill("dance", "Make the robot dance in a circle", requires=["base.drive"])
def dance(robot, speed=0.2, turns=4):
    import time
    for _ in range(turns):
        robot.drive(vx=speed, w=0.8)
        time.sleep(3.14)
    robot.stop()
    return f"danced {turns} turns at {speed} m/s"
```

Then use it:

```bash
ohho market list          # "dance" now appears
ohho market run omnibot dance
```

Or in Python:

```python
from ohho.market import run_skill
run_skill("dance", bot, speed=0.3, turns=2)
```

## Capability gating

Skills declare which capabilities they `requires`. When you run a skill on a
robot that lacks those capabilities, you get a clear error:

```python
from ohho.market import run_skill, SkillRequirementsNotMet

try:
    run_skill("wave", dog)  # Go2 has no manipulation
except SkillRequirementsNotMet as e:
    print(e)
    # skill 'wave' requires capabilities: ['manipulation'].
    # Robot 'unitree-go2' has: ['base.drive', 'base.holonomic_drive', ...]
```

## The Skill dataclass

```python
from ohho.market import Skill, register_skill

custom = Skill(
    name="inspect",
    description="Drive forward and report obstacles",
    handler=lambda robot: robot.drive(vx=0.1),
    requires=["base.drive"],
    author="your-name",
    version="1.0.0",
    tags=["inspection", "safety"],
)
register_skill(custom)
```

## Next

- [Architecture](architecture.md) — how skills fit into the engine
- [Contributing](https://github.com/varunvaidhiya/OmniBotPro/blob/main/sdk/CONTRIBUTING.md) — add your own robot
