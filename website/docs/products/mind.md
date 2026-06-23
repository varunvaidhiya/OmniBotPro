# OhhO Mind

**Give your robot a mind of its own.**

- **Category:** Intelligence
- **Accent:** violet
- **Live app:** [Open the console](/mind)

The continuous agent brain that turns a command-taking robot into a goal-driven
one. It perceives, reasons, verifies, acts, reflects and learns — on a loop — and
ships to your fleet as an over-the-air update.

## Overview (hero)

OhhO Autonomy lets a robot follow a mission; OhhO Mind lets it decide on one. Mind
is the deliberative layer that runs continuously on top of your stack — perceive →
reason → verify → act → monitor → reflect → remember — turning a capable robot
into an agent you give goals to, not scripts.

## Highlights

- Continuous perceive→reason→act→reflect loop
- Goals in plain language, not scripted commands
- Hardware-safety gate on every action
- Hybrid reasoning: cloud + on-device + NPU
- Memory + learns from its own experience

## What you get

- Foundation models gave robots brilliant reflexes — a Vision-Language-Action
  model turns an image and an instruction into an action. But a reflex isn't a
  mind. A robot still can't choose a goal, remember what it saw, notice that it
  failed, or get better over time. OhhO Mind is the layer that closes that gap.
- Mind runs a continuous decision loop on top of the robot's existing fast control
  stack. It fuses everything the robot knows into one world-state snapshot,
  reasons over it to pick the next action, verifies that action against the
  robot's real hardware limits, executes it through OhhO Autonomy and OhhO Serve,
  watches the outcome, and reflects on whether the goal was met — then remembers
  what it learned and does it again. You hand it an objective in plain language;
  it loops until the job is done, then idles.
- Crucially, Mind is built for the real world, not the demo table. Its reasoning
  runs wherever it can — cloud models when connected, an on-device model and an
  onboard NPU when not — so the robot keeps thinking with no internet. And because
  it judges and stores every attempt, that experience feeds OhhO Train to improve
  the underlying policies. Mind reaches your robots the way software should: as an
  OhhO Fleet over-the-air update, so a deployed fleet wakes up smarter with no new
  hardware.

## Features

- **The agent loop** — A continuous perceive → reason → verify → act → monitor →
  reflect → remember cycle. Give it a goal; it runs until done, recovers from
  failure, and idles when there's nothing to do.
- **Grounded world state** — Mind fuses pose, arm state, detected objects, mission
  status and memory into one snapshot — so it reasons about the world the robot is
  actually in, not a guessed one.
- **A safety gate it can't skip** — Every low-level action is checked against the
  robot's real limits — velocity, joint deltas, reach — before it reaches a motor.
  Unsafe plans are rejected, not clipped.
- **Hybrid reasoning** — Cloud-class reasoning when online, an on-device model and
  NPU when offline. The robot degrades gracefully instead of going dark when the
  network does.
- **Memory that compounds** — Mind remembers objects, places and the outcomes of
  past attempts, and feeds them into every decision — so the robot builds a model
  of its own environment.
- **Learns from experience** — Each attempt is judged, labelled and stored, then
  fed to OhhO Train to improve the policies — closing the loop from operation back
  to capability.

## How it works

1. **Give it a goal** — Send a plain-language objective — 'find the red cup and
   bring it back' — instead of a step-by-step script.
2. **It reasons & verifies** — Mind grounds the goal in the live world state,
   picks the next action, and passes it through the safety gate.
3. **It acts & reflects** — It runs the action through Autonomy and Serve, watches
   the result, and re-plans until the goal is met.
4. **It learns & updates** — Outcomes feed OhhO Train; improved intelligence ships
   back to the fleet as an OhhO Fleet OTA update.

## Specs

| Spec | Value |
|---|---|
| Loop | perceive → reason → verify → act → monitor → reflect → remember |
| Reasoning | Cloud LLM + on-device LLM + NPU (hybrid router) |
| Safety | Hardware-limit + reachability gate on every action |
| Memory | Object / place memory + episodic recall |
| Learning | Judged episodes → OhhO Train continual learning |
| Delivery | OhhO Fleet over-the-air update |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | Single-robot agent loop | ✅ |
| Fleet | + hybrid reasoning, memory & continual learning | ✅ |
| Forge | On-prem brain + custom skills | ✅ |

**Recommended plan: Fleet.** Builder runs the agent loop on a single robot —
enough to give one machine goals instead of scripts. Choose Fleet for the full
hybrid (offline) reasoning, long-term memory, continual learning and OTA delivery
across a fleet; Forge runs the brain entirely on-prem with your own custom skills.

## FAQ

**How is this different from OhhO Autonomy?**
Autonomy executes a mission — map, navigate, run a skill. Mind decides the
mission: it chooses goals, sequences skills, recovers from failure and learns.
Mind sits on top of Autonomy and commands it.

**Does it need the cloud to think?**
No. Mind prefers cloud-class reasoning when connected, but falls back to an
on-device model and an onboard NPU when offline — so the robot keeps operating
with no internet.

**Is it safe to let a robot decide for itself?**
Every action Mind emits passes a safety gate checked against the robot's real
hardware limits before it reaches a motor, and a human can take over or e-stop at
any moment.

**How does it reach robots already in the field?**
Mind is delivered as an OhhO Fleet over-the-air update — your deployed robots
receive it like an app update, with no new hardware and no teardown.

## Related products

- [OhhO Autonomy](./autonomy.md)
- [OhhO Serve](./serve.md)
- [OhhO Train](./train.md)
