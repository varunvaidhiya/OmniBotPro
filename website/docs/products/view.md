# OhhO View

**Four cameras. One smart view.**

- **Category:** Intelligence
- **Accent:** violet
- **Live app:** [Open perception viewer](/view)

Fuse base-mounted cameras into a single calibrated bird's-eye-view your robot and
your operators can both rely on. CPU-only, ROS 2-ready.

## Overview (hero)

Four cameras, one smart view. Fuse base-mounted cameras into a single calibrated
bird's-eye-view your robot and your operators can both rely on — CPU-only, ROS
2-ready.

## Highlights

- 4-camera surround BEV
- Geometric IPM, true top-down
- CPU-only — no GPU needed
- Calibration UI included
- ROS 2 topic out of the box

## What you get

- A robot that can only see straight ahead is a robot that bumps into things.
  OhhO View stitches up to four base-mounted cameras into one top-down, surround
  bird's-eye-view (BEV) of everything around the robot.
- View computes geometric inverse-perspective-mapping from your robot's camera
  poses, producing a true fused top-down image — not a 2×2 tile. A calibration UI
  dials it in, and it runs on the CPU, so it doesn't compete with your GPU for VLA
  inference.
- The BEV feeds both humans and models: operators get instant situational
  awareness, and policies like SmolVLA consume the same surround view as an input
  — closing the loop between perception and action.

## Features

- **True fused BEV** — Geometric inverse-perspective-mapping from URDF camera
  poses produces a single top-down image, not a tiled mosaic.
- **Calibration UI** — A bring-up wizard aligns the cameras; a calibration file
  takes priority when you want pixel-perfect.
- **CPU-only** — Runs on the robot's compute without touching the GPU your models
  need.
- **Model-ready output** — Publishes a standard ROS 2 image topic consumed by
  OhhO Serve policies and recorders alike.
- **Any camera layout** — Configure front / rear / left / right poses, field of
  view and mounting height for your specific robot.

## How it works

1. **Mount & describe** — Add four cameras and their poses — or import them from
   an OhhO Build design.
2. **Calibrate** — Run the calibration UI to align the surround view.
3. **Publish the BEV** — View streams a fused top-down image on a ROS 2 topic.
4. **Feed perception & policy** — Operators and models consume the same view.

## Specs

| Spec | Value |
|---|---|
| Cameras | Up to 4 (front / rear / left / right) |
| Method | Geometric IPM from URDF poses |
| Compute | CPU-only |
| Calibration | UI + optional calibration file |
| Output | ROS 2 Image topic (BEV) |
| Consumers | OhhO Serve, OhhO Data, operators |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Open source, included | ✅ |
| Builder | Included | ✅ |
| Fleet | Included + multi-robot calibration | ✅ |
| Forge | Included + custom rigs | ✅ |

**Recommended plan: Spark.** View is open source and included on every plan,
including free. There's nothing to buy to get the surround view running — pick the
plan you need for the rest of the stack.

## FAQ

**Do I need a depth camera?**
No. View works with standard RGB cameras; depth is optional and used elsewhere in
the stack.

**Will it slow down my AI?**
No — View is CPU-only by design, leaving the GPU free for OhhO Serve.

## Related products

- [OhhO Build](./build.md)
- [OhhO Serve](./serve.md)
- [OhhO Pilot](./pilot.md)
