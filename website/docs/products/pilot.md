# OhhO Pilot

**Operate any robot. From anywhere.**

- **Category:** Operations
- **Accent:** violet
- **Live app:** [Open the cockpit](/pilot)

VR and mobile teleoperation in real time. Pre-loaded robot profiles, a custom
robot builder, and arm control via hand tracking.

## Overview (hero)

Operate any robot, from anywhere. VR and mobile teleoperation with pre-loaded
robot profiles, a custom robot builder, and arm control via hand tracking —
real-time and low-latency.

## Highlights

- VR + mobile teleop
- Hand-tracking arm IK
- Pre-loaded robot profiles
- Live camera view (MJPEG)
- Emergency stop + safe clamps

## What you get

- Sometimes the robot needs a human. OhhO Pilot is the teleoperation cockpit —
  drive a mobile base, command an arm, and see what the robot sees, from a phone
  or a VR headset, anywhere in the world.
- Pilot ships with pre-loaded profiles for common robots and a custom builder for
  your own. In VR, arm inverse-kinematics is driven by natural hand tracking; on
  mobile, an on-screen joystick and live camera view keep you in control.
- It connects over the same standard ROSBridge WebSocket your stack already speaks
  — with safe velocity clamping, an emergency stop, and a control-mode mux so
  teleop, navigation and AI never fight over the wheels.

## Features

- **VR cockpit** — Drive and manipulate in immersive VR; arm IK follows your
  hands.
- **Mobile app** — On-screen joystick, live video and mission controls in your
  pocket.
- **Robot profiles** — Start from pre-loaded profiles or build your own to match
  your hardware.
- **Safe by construction** — Velocity clamps, an emergency stop and a control-mode
  mux prevent conflicting commands.
- **Standard transport** — Connects via ROSBridge WebSocket — no custom firmware
  required.

## How it works

1. **Connect** — Point Pilot at your robot's ROSBridge endpoint.
2. **Pick a profile** — Load a robot profile or build one for your hardware.
3. **Take control** — Drive and manipulate from mobile or VR with live video.
4. **Hand off to AI** — Switch control modes between teleop, navigation and AI
   policies.

## Specs

| Spec | Value |
|---|---|
| Platforms | VR headset + mobile (Android) |
| Transport | ROSBridge WebSocket (port 9090) |
| Arm control | Hand-tracking IK (VR), joint UI (mobile) |
| Video | MJPEG via web_video_server |
| Safety | Velocity clamps, e-stop, control-mode mux |
| Robots | Pre-loaded profiles + custom builder |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | Mobile, up to 3 robots | ✅ |
| Fleet | VR + mobile, unlimited | ✅ |
| Forge | White-label Pilot | ✅ |

**Recommended plan: Fleet.** Casual mobile teleop of a few robots fits Builder.
For VR, unlimited robots, or to put Pilot in front of your own operators, choose
Fleet — or Forge to white-label it.

## FAQ

**Do I need a VR headset?**
No. Pilot works fully on mobile; VR is an option for immersive arm control.

**Is it safe over the internet?**
Pilot clamps velocities and offers an emergency stop; pair it with OhhO Shield for
authenticated, encrypted links.

## Related products

- [OhhO Autonomy](./autonomy.md)
- [OhhO View](./view.md)
- [OhhO Fleet](./fleet.md)
- [OhhO Connect](./connect.md)
