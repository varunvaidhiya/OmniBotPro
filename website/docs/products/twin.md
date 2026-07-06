# OhhO Twin

**Your real robot. Mirrored in simulation. Live.**

- **Category:** Operations
- **Accent:** violet
- **Live app:** [Open the twin](/twin)

A live digital twin that streams real robot telemetry into a persistent simulation
— replay, scrub, what-if and predict, side by side with the physical robot.
Powered by Gazebo and Isaac Sim.

## Overview (hero)

OhhO Frame gives you a simulator to develop against; OhhO Twin gives you a live
mirror of the robot you're already running. Real telemetry streams into a
persistent Gazebo or Isaac Sim world — so you can replay the last hour, scrub to
the moment something went wrong, run a what-if with a different policy, and predict
what happens next.

## Highlights

- Live telemetry → persistent sim
- Replay + scrub any moment
- What-if with different policies
- Prediction from observed state
- Gazebo + Isaac Sim (USD) backed
- OPC UA factory integration

## What you get

- A digital twin is the bridge between 'it worked in simulation' and 'it's working
  right now on the factory floor.' OhhO Twin streams a real robot's telemetry —
  pose, joints, sensors, camera frames — into a persistent simulation world that
  stays in sync, so the sim always reflects what the robot is actually doing.
- Twin is not just a live view. It's a time machine: every telemetry frame is
  recorded, so you can replay the last shift, scrub to the moment a pick failed,
  and see exactly what the robot saw and felt at that instant. Run a what-if —
  'what if the policy had chosen a different grasp angle?' — and Twin simulates the
  alternative from the same starting state.
- For fleet operators, Twin is the difference between reactive and predictive. A
  motor's temperature curve in the twin predicts a failure before it happens. A
  near-miss in the twin becomes a training scenario for OhhO Proof. And because
  Twin runs on the same Gazebo and Isaac Sim worlds as Frame, what you learn in the
  twin transfers directly to the simulation you develop in.

## Features

- **Live mirror** — Real robot telemetry streams into a persistent Gazebo or Isaac
  Sim world, kept in sync frame by frame — so the sim always reflects reality.
- **Replay + scrub** — Every telemetry frame is recorded. Replay the last hour,
  scrub to any instant, and inspect pose, joints, sensors and camera frames at
  that exact moment.
- **What-if simulation** — Branch from any recorded state and simulate a different
  outcome — a different policy, a different grasp, a different speed — without
  touching the real robot.
- **Prediction** — From the observed state, Twin can project forward — motor
  temperature trends, battery depletion, trajectory completion — so you see
  problems before they happen.
- **Shared sim world** — Twin runs on the same Gazebo and Isaac Sim worlds as OhhO
  Frame (URDF, SDF and USD scene formats), so what you learn in the twin transfers
  directly to the simulation you develop and test in.
- **OPC UA factory integration** — Twin speaks OPC UA — the Industry 4.0 standard
  — so your digital twin exchanges data with factory cells, MES/SCADA systems and
  enterprise digital twin platforms that already speak OPC UA. A robot on OhhO
  plugs into the digital twin infrastructure your plant already has.
- **Fleet-scale** — Mirror one robot or a hundred. Each twin streams independently
  and is replayable from the Fleet dashboard.

## How it works

1. **Connect the robot** — Twin uses the live telemetry stream from OhhO Connect —
   no extra wiring.
2. **Mirror in sim** — Real telemetry flows into a persistent Gazebo or Isaac Sim
   world that stays in sync.
3. **Replay + what-if** — Scrub to any moment, inspect what the robot saw, and
   branch into a what-if simulation.
4. **Predict + feed back** — Twin projects trends forward — and near-misses become
   OhhO Proof scenarios, failures become OhhO Care tickets.

## Specs

| Spec | Value |
|---|---|
| Simulators | Gazebo Harmonic + Isaac Sim |
| Scene formats | USD, SDF, URDF |
| Factory integration | OPC UA (Industry 4.0) |
| Telemetry | Pose, joints, IMU, cameras (via OhhO Connect) |
| Replay | Full timeline scrub, per-frame inspection |
| What-if | Branch from any recorded state |
| Prediction | Motor, battery, trajectory projection |
| Scale | Single robot (Builder) to fleet (Fleet) |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Simulated twin (no live data) | ✅ |
| Builder | Single live twin + replay | ✅ |
| Fleet | Multi-robot twins + what-if | ✅ |
| Forge | Enterprise twin + prediction APIs | ✅ |

**Recommended plan: Builder.** Spark gives you a simulated twin to explore the
concept. Most teams want Builder for a single live twin with replay — enough to
diagnose what happened on the real robot. Fleet operators choose Fleet for
multi-robot twins and what-if; enterprises choose Forge for prediction APIs and
custom models.

## FAQ

**How is Twin different from Frame's simulation?**
Frame gives you a simulator to develop against — a clean world you launch and
iterate in. Twin mirrors a real robot that's already running, streaming live
telemetry and recording every frame for replay and what-if. They share the same
Gazebo and Isaac Sim worlds.

**Do I need hardware to use Twin?**
No. Spark includes a simulated twin with synthetic telemetry. But the real value —
replay, what-if, prediction — comes from streaming a real robot's data, which
starts on Builder.

**Can Twin predict failures?**
Yes. By tracking motor temperature, current draw and vibration trends in the
recorded telemetry, Twin projects degradation forward — and feeds OhhO Care to
schedule maintenance before a failure.

## Related products

- [OhhO Frame](./frame.md)
- [OhhO Fleet](./fleet.md)
- [OhhO Care](./care.md)
