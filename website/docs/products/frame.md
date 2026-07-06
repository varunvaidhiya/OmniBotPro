# OhhO Frame

**Your robot stack, ready in one afternoon.**

- **Category:** Foundation
- **Accent:** violet
- **Live app:** [Open device console](/frame)

A production-grade robot software foundation. Docker, ROS 2, simulation and
CI/CD — pre-wired. Single or multi-machine deployments out of the box.

## Overview (hero)

The robot software stack, pre-wired. Docker, ROS 2, simulation, CI/CD and
multi-machine deployment — a production-grade foundation you launch in an
afternoon instead of assembling over months.

## Highlights

- ROS 2 Jazzy workspace, pre-structured
- Docker + DevContainer build
- Gazebo & Isaac simulation (USD + SDF)
- MCAP recording format (ROS 2 bags)
- CI/CD pipeline ready
- Single or multi-machine deploy

## What you get

- Every robot project starts by rebuilding the same plumbing: a ROS 2 workspace,
  Docker images, a simulator, message buses, launch files, a CI pipeline. OhhO
  Frame gives you all of it, already wired together and known-good.
- Frame is the chassis your robot's software rides on. It ships the workspace
  layout, containerized build, Gazebo and Isaac simulation, and single- or
  multi-machine deployment — robot plus GPU workstation — that the rest of the
  OhhO products plug straight into.
- Built in the open on ROS 2 Jazzy, Frame is the same foundation that powers
  OhhO's own reference robot — so what you start from is a real, tested
  production stack, not a toy template.

## Features

- **Batteries-included workspace** — Driver, description (URDF), navigation,
  perception and bringup packages laid out the way a production robot needs them.
- **Containerized everything** — Docker images and a DevContainer so 'works on my
  machine' becomes 'works on every machine' — including CI.
- **Simulate before you build** — Gazebo Harmonic and Isaac Sim worlds wired to
  the same topics as the real robot, so you develop with no hardware. Robot
  description uses URDF and SDF; Isaac Sim scenes use USD (Universal Scene
  Description) — the same 3D standard Pixar, Omniverse and the digital twin
  industry settled on.
- **MCAP recording** — Frame records ROS 2 data in MCAP — the open-source,
  ROS 2-native bag format — so your logs interoperate with the wider ROS 2
  ecosystem's tooling, not a proprietary format.
- **CI/CD out of the box** — A GitHub Actions pipeline builds the workspace and
  runs the test suite on every push.
- **Single or multi-machine** — One configurator switches between
  all-on-one-workstation and Pi-robot + GPU-desktop topologies, wiring DDS peers
  for you.

## How it works

1. **Generate your stack** — Frame scaffolds the workspace, Docker, sim and CI
   from your robot profile — or directly from an OhhO Build export.
2. **Develop in simulation** — Launch the simulator and iterate on navigation,
   perception and control with no hardware.
3. **Deploy single or multi-machine** — Pick your topology; Frame wires DDS and
   the launch files.
4. **Layer on the platform** — Add View, Serve, Data, Fleet and the rest on top
   of the same foundation.

## Specs

| Spec | Value |
|---|---|
| ROS distro | ROS 2 Jazzy (Ubuntu 24.04) |
| Containers | Docker + DevContainer |
| Simulation | Gazebo Harmonic + Isaac Sim |
| Scene formats | URDF, SDF, USD (Isaac Sim / Omniverse) |
| Recording | MCAP (ROS 2-native bag format) |
| CI | GitHub Actions (build + colcon test) |
| Deploy modes | Single workstation / multi-machine |
| Networking | DDS peer auto-config (ROS_DOMAIN_ID 30) |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Simulation only | ✅ |
| Builder | Single-machine + hardware | ✅ |
| Fleet | Multi-machine deploy | ✅ |
| Forge | On-prem + custom topologies | ✅ |

**Recommended plan: Spark.** Frame is included on every plan — even free. Start
on Spark to build and simulate at no cost; you only need a paid plan once you're
deploying to real hardware or multiple machines.

## FAQ

**Do I need hardware to start?**
No. Frame's simulation runs entirely on your workstation — many teams build for
weeks before touching a robot.

**Is it locked to your hardware?**
No. Frame works with any ROS 2-compatible hardware; the reference drivers are a
starting point you can swap.

## Related products

- [OhhO Build](./build.md)
- [OhhO Bench](./bench.md)
- [OhhO Fleet](./fleet.md)
- [OhhO Connect](./connect.md)
