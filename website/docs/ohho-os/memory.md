# Spatio-Temporal Memory & Perception

OhhO OS gives every robot a persistent, queryable memory of **what** it has
seen, **where**, and **when** (`ohho.memory`), fed by pluggable perception
(`ohho.perception`). Pure standard library; persists as JSON per robot at
`~/.ohho/memory/<robot>.json`, shared by the CLI and the agent.

- **Object permanence** — a sighting of the same label near a known position
  updates that entity instead of creating a duplicate
- **Spatial queries** — *where is the cup? what's near (1, 2)?*
- **Temporal queries** — event timeline, "last seen 42 s ago", free-form notes
- **Perceptors** — a deterministic sim perceptor (FOV + range over the sim
  world) and an optional **Claude-vision perceptor** for real camera frames

## CLI

```bash
# Perceive the surroundings and remember them
ohho look omnibot --transport sim:// --world demo
#   chair at (1.50, 0.00) (conf 0.79)
#   table at (2.50, 1.20) (conf 0.60)
# remembered 2 object(s) → ~/.ohho/memory/omnibot.json

# Recall — works in a later session, no robot connection needed
ohho memory where omnibot chair
# chair last seen at (1.50, 0.00), 312s ago (seen 1×)

ohho memory show omnibot        # summary + recent events
ohho memory near omnibot --x 2.0 --y 1.0 --radius 1.5
ohho memory clear omnibot
```

## Python

```python
from ohho import Robot
from ohho.memory import SpatialMemory, default_memory_path
from ohho.perception import SimPerceptor, remember_detections

bot = Robot.connect("omnibot")
memory = SpatialMemory(path=default_memory_path("omnibot"))

detections = SimPerceptor(bot).look()          # sim world, FOV + range gated
remember_detections(memory, detections)

e = memory.where_is("cup")                     # Entity(x, y, last_seen, count…)
memory.near(1.0, 2.0, radius=1.5)              # entities around a point
memory.timeline(label="cup")                   # every sighting, time-stamped
memory.note("picked up the cup")               # free-form events
memory.save()
```

### Claude-vision perception (real cameras)

With the `[agent]` extra and `ANTHROPIC_API_KEY`, `VlmPerceptor` turns camera
frames into detections:

```python
from ohho.perception import VlmPerceptor

vlm = VlmPerceptor()                       # model="claude-sonnet-5" by default
detections = vlm.look(jpeg_bytes)          # [Detection(label, confidence), …]
```

## Agent integration

The agent's tool registry includes the memory suite automatically:
`look_around()` (perceive + remember + report), `where_is(label)`,
`objects_near(x, y, radius)`, and `remember_note(text)` — and the robot's
memory summary is injected into the agent's context at the start of every
goal, so "go back to where you saw the cup" works across sessions.
