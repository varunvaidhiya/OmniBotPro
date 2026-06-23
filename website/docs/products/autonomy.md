# OhhO Autonomy

**Map it, navigate it, command it in plain language.**

- **Category:** Intelligence
- **Accent:** violet
- **Live app:** [Open mission control](/autonomy)

The robot's autonomy stack — SLAM mapping, Nav2 navigation and a mission planner
— driven by a natural-language agent that turns "tidy the kitchen" into a sequence
of robot actions.

## Overview (hero)

Teleop is for when a human drives; Autonomy is for when the robot drives itself.
It maps a space with SLAM, navigates it with Nav2, and runs a mission planner that
sequences navigation and manipulation — all orchestrated by a natural-language
agent that turns an instruction into a plan.

## Highlights

- 2-D & 3-D SLAM mapping
- Nav2 path planning + obstacle avoidance
- Mission planner (navigate → act sequences)
- Natural-language agent (Claude-backed)
- Safe control-mode mux: nav / AI / teleop

## What you get

- Perception tells a robot what's around it; Autonomy decides what to do about it.
  OhhO Autonomy is the behavior layer that turns a powered-on robot into one that
  moves through the world and completes tasks on its own.
- It builds and localizes against a map with SLAM (2-D and 3-D), plans
  collision-free paths with Nav2 over a fused costmap, and fuses wheel odometry
  and IMU through an EKF for reliable pose. A mission planner sequences
  higher-level jobs — navigate to a named location, then run a manipulation policy
  — and a control-mode mux arbitrates cleanly between navigation, AI policies and
  human teleop so they never fight over the wheels.
- On top sits a natural-language agent, backed by Claude, that turns 'take the red
  cup from the kitchen to the bench' into a structured mission: it knows your
  named locations, can describe what it sees, and asks for clarification when an
  instruction is ambiguous — then hands the mission to the planner to execute.

## Features

- **SLAM mapping** — Build and localize against 2-D and 3-D maps; switch between
  mapping and localization modes against a saved map.
- **Nav2 navigation** — Collision-free path planning and obstacle avoidance over a
  fused costmap, tuned to the robot's real velocity and acceleration limits.
- **Robust localization** — An EKF fuses wheel odometry and IMU so pose stays
  trustworthy even when mecanum wheels slip.
- **Mission planner** — Sequence navigation and manipulation into a mission — 'go
  to the kitchen, then pick up the cup' — as a tracked state machine.
- **Natural-language agent** — A Claude-backed agent maps plain-language
  instructions to missions, with named locations, scene description and
  clarification when it's unsure.
- **Safe arbitration** — A control-mode mux switches between navigation, AI
  policies and teleop so commands never conflict — and a human can always take
  over.

## How it works

1. **Map the space** — Drive once while SLAM builds a map, then save it for
   localization.
2. **Name the places** — Tag locations — kitchen, dock, bench — the planner and
   agent can refer to.
3. **Give an instruction** — Type or speak a task; the agent turns it into a
   structured mission.
4. **Watch it execute** — Nav2 and the policies run the mission; the mux keeps
   teleop override one tap away.

## Specs

| Spec | Value |
|---|---|
| Mapping | 2-D & 3-D SLAM (mapping / localization) |
| Navigation | Nav2 planner + costmap obstacle avoidance |
| Localization | EKF fusing wheel odometry + IMU |
| Missions | Navigate → manipulate state machine |
| Agent | Natural language → mission (Claude-backed) |
| Arbitration | Control-mode mux: nav / AI / teleop |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | SLAM + navigation in simulation | ✅ |
| Builder | + mission planner on real hardware | ✅ |
| Fleet | + natural-language agent & named missions | ✅ |
| Forge | Custom behaviors + on-prem agent | ✅ |

**Recommended plan: Builder.** Builder gives you mapping, navigation and the
mission planner on real hardware — the core of autonomy. Add Fleet when you want
the natural-language agent and reusable named missions; Forge is for custom
behaviors and running the agent on-prem.

## FAQ

**How is this different from OhhO Pilot?**
Pilot is for a human operating the robot; Autonomy is for the robot operating
itself. They share the same safe control-mode mux, so you can hand control back
and forth instantly.

**Does the language agent need the cloud?**
By default it calls Claude, but the agent layer is optional — navigation and the
mission planner run fully on-robot, and Forge can run the agent on-prem.

## Related products

- [OhhO Serve](./serve.md)
- [OhhO View](./view.md)
- [OhhO Pilot](./pilot.md)
