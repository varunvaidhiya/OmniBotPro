# Native Navigation

OhhO OS ships a **built-in, no-ROS navigation stack** (`ohho.nav`) — the
lightweight counterpart to Nav2's core loop, pure standard library, running on
any robot with `base.drive`:

- **Occupancy-grid mapping** from the robot's planar scan (`perception.scan`)
- **Costmap** obstacle inflation by the robot's radius
- **A\*** path planning (8-connected, replans while driving)
- **Frontier exploration** — drive into the unknown until the map is complete
- A **closed-loop Navigator** with collision-aware stuck detection and
  goal diversion ("go to the table" stops *beside* the table)

For full Nav2 / SLAM-toolbox navigation, use the ROS 2 runtime instead — this
stack is for the no-ROS path and for robots without a ROS stack.

## CLI

```bash
# Build and print the map (spin-scan in place)
ohho nav map sim --transport sim:// --world demo

# Navigate to a world-frame point (A* over the live map, replanning)
ohho nav goto sim --transport sim:// --world demo --x 2.0 --y 1.0
# reached at (1.78, 0.84), 0.14 m from goal

# Frontier exploration
ohho nav explore sim --transport sim:// --world demo --frontiers 4
```

`--world demo` furnishes the simulator with labeled obstacles (chair, table,
plant, box, cup). On a real robot, the map builds from the robot's scan.

## Python

```python
from ohho import Robot
from ohho.nav import Navigator

bot = Robot.connect("omnibot")
nav = Navigator(bot)

result = nav.navigate_to(2.0, 1.0)      # NavResult(reached, reason, x, y, …)
print(result)                            # reached at (1.98, 1.02), 0.05 m from goal

for line in nav.explore(max_frontiers=4):
    print(line)

print(nav.map_ascii())                   # '#' occupied · '·' free · ' ' unknown
```

Deterministic testing: every loop accepts `step_fn(dt)` — pass the sim's
`step` to advance physics synchronously (no threads, no sleeps).

## Behavior details

- **Goal inside an obstacle** → the navigator diverts to the nearest reachable
  free cell and reports `goal (adjusted)`.
- **Stuck against something unmapped** → after ~1.2 s without progress it
  either declares arrival (if already beside the target) or bump-marks the
  blocking cell and replans around it.
- **No scan capability** → the planner is optimistic over unknown space
  (straight-line A*); mapping simply stays empty.
- Holonomic bases strafe toward waypoints; differential bases rotate-then-drive.

## Agent tools

When the agent runs (`ohho agent …`), navigation is exposed as tools the LLM
can call: `navigate_to(x, y)` and `explore()` — see [Memory](memory.md) for
the perception/recall tools that pair with them.
