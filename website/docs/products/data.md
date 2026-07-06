# OhhO Data

**Collect. Label. Ship.**

- **Category:** Intelligence
- **Accent:** cyan
- **Live app:** [Open episode viewer](/data)

An end-to-end pipeline for robot demonstration data. Teleop recording, an episode
viewer and CLI tools — in LeRobot-compatible format, training-ready.

## Overview (hero)

Collect, label and ship robot demonstration data. An end-to-end pipeline in
LeRobot-compatible format, with teleop recording, an episode viewer and CLI tools
— the fuel your policies learn from.

## Highlights

- Teleop episode recording
- LeRobot dataset format (Parquet + MP4)
- MCAP recording (ROS 2 bags)
- Multi-camera time sync
- Episode viewer & curation
- CLI tools, training-ready

## What you get

- Robot intelligence is only as good as the demonstrations it learns from. OhhO
  Data is the pipeline that turns teleoperation sessions into clean,
  training-ready datasets.
- Record episodes from a leader arm and mobile base, synchronized with
  multi-camera video, and store them in the standard LeRobot dataset format —
  Parquet plus MP4. Review and curate with the episode viewer, then ship straight
  to training.
- Data is designed for the whole loop: the same schema your robot records is the
  schema your model trains on and the policy runs in production — so what you
  collect is exactly what you deploy.

## Features

- **Synchronized recording** — Leader arm, base velocity and multiple camera
  streams aligned to a tight sync tolerance, frame by frame.
- **Standard format** — LeRobot-compatible Hugging Face datasets (Parquet + MP4)
  for imitation learning, plus MCAP — the ROS 2-native bag format — for raw ROS 2
  topic recording. No bespoke converters, no lock-in.
- **Episode viewer** — Scrub, inspect and keep-or-discard episodes before they
  pollute a training run.
- **One schema, end to end** — A 9-DOF mobile-manipulation state/action that
  matches the recorder, the trainer and the policy.
- **CLI-first** — Scriptable record / inspect / push commands that fit into a
  data-ops workflow.

## How it works

1. **Teleoperate** — Drive the robot — or the simulator — with a leader arm and
   joystick while Data records.
2. **Review** — Open the episode viewer and discard the bad takes.
3. **Ship** — Push the dataset in LeRobot format to training.
4. **Close the loop** — Fine-tune with the OmniVLA engine and deploy via OhhO
   Serve.

## Specs

| Spec | Value |
|---|---|
| Training format | LeRobot HF dataset (Parquet + MP4) |
| ROS 2 recording | MCAP (ROS 2-native bag format) |
| State / action | 9-DOF (arm ×6 + base ×3) |
| Cameras | Front + wrist + BEV, time-synced |
| Sync tolerance | ~50 ms |
| Tools | Recorder node, episode viewer, CLI |
| Targets | Real robot, Gazebo, Isaac Sim |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Local recording | ✅ |
| Builder | Cloud sync, 1K episodes | ✅ |
| Fleet | Unlimited + annotation | ✅ |
| Forge | Unlimited + managed pipeline | ✅ |

**Recommended plan: Builder.** Builder adds cloud sync and 1,000 episodes —
enough to train a first real policy. Teams collecting at scale or labeling heavily
should move to Fleet for unlimited episodes and annotation.

## FAQ

**Is my data portable?**
Completely. It's standard LeRobot format — train with OhhO's engine or any
compatible toolchain.

**Can I collect in simulation?**
Yes. Data records from Gazebo and Isaac Sim with the same schema as the real
robot.

## Related products

- [OhhO Train](./train.md)
- [OhhO Serve](./serve.md)
- [OhhO Pilot](./pilot.md)
