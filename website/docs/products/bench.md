# OhhO Bench

**From a box of parts to a robot that powers on.**

- **Category:** Foundation
- **Accent:** cyan
- **Live app:** [Open bring-up console](/bench)

Guided assembly, wiring and firmware bring-up. Turn an OhhO Build bill of
materials into a wired, flashed and calibrated robot — with step-by-step
instructions and hardware self-tests.

## Overview (hero)

Design is the easy half. OhhO Bench takes the bill of materials you exported from
Build and walks you all the way to a robot that powers on — guided mechanical
assembly, a wiring map, firmware flashing, and hardware self-tests that prove
every motor, sensor and servo actually works.

## Highlights

- Step-by-step assembly from your BOM
- Wiring & port map (serial / power / bus)
- One-click firmware flashing
- Hardware self-test & diagnostics
- Sensor, camera and arm calibration

## What you get

- Between a sourced bill of materials and a working software stack lies the part
  nobody writes documentation for: bolting the robot together, wiring the motor
  board, flashing firmware, and discovering — usually the hard way — which
  connector is in backwards. OhhO Bench turns that into a guided, checked
  workflow.
- Bench reads your OhhO Build design and generates the exact assembly order, a
  wiring and port map (which controller talks over which serial port at which
  baud rate), and a firmware bring-up sequence. As you go, it runs hardware
  self-tests — spin each motor, read the encoders and IMU, sweep every arm
  servo — so a mistake is caught at the bench, not in the field.
- When the mechanics are sound, Bench runs the calibration routines: motor
  direction and odometry geometry, IMU bias, camera intrinsics and the
  surround-view rig, and arm joint homing. It writes the deployment profile the
  rest of the stack consumes — so the moment Bench turns green, OhhO Frame, View
  and Autonomy come up on real, calibrated hardware.

## Features

- **Assembly from your design** — Bench expands your Build BOM into an ordered,
  illustrated assembly sequence — what bolts to what, in what order, with torque
  and orientation called out.
- **Wiring & port map** — A generated harness diagram: motor board, arm bus,
  cameras and compute, with the serial ports, baud rates and power budget each
  one needs.
- **Firmware flashing** — Flash the motor-controller and microcontroller firmware
  from the browser, with the right protocol and car-type set for your base — no
  hand-edited config.
- **Hardware self-test** — Spin each wheel, read encoders and IMU, and sweep every
  arm joint to confirm wiring and direction before any autonomy runs.
- **Guided calibration** — Walk through odometry geometry, IMU bias, camera
  intrinsics, the surround-view rig and arm homing — and write them into the
  deployment profile.
- **Hands off to the stack** — A green Bench produces the deployment profile and
  calibration files that OhhO Frame, View and Autonomy pick up with zero
  re-entry.

## How it works

1. **Import the build** — Bench pulls the BOM, wiring and component specs from
   your OhhO Build design.
2. **Assemble & wire** — Follow the ordered assembly steps and the generated
   wiring / port map.
3. **Flash & self-test** — Flash firmware and run the hardware self-tests until
   every subsystem reports healthy.
4. **Calibrate & hand off** — Run the calibration routines; Bench writes the
   deployment profile for the rest of the stack.

## Specs

| Spec | Value |
|---|---|
| Input | OhhO Build BOM + wiring + component specs |
| Firmware | Motor board + MCU, protocol-aware flashing |
| Self-test | Motors, encoders, IMU, arm servos, cameras |
| Calibration | Odometry, IMU bias, camera intrinsics, BEV rig, arm homing |
| Output | Deployment profile + calibration files |
| Handoff | OhhO Frame, View, Autonomy |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Assembly guide + self-test | ✅ |
| Builder | + firmware flashing & calibration | ✅ |
| Fleet | + batch bring-up across many units | ✅ |
| Forge | Contract-manufacturing handoff pack | ✅ |

**Recommended plan: Builder.** Anyone building their first unit wants Builder for
firmware flashing and the guided calibration routines. Teams bringing up many
identical robots move to Fleet for batch bring-up, and manufacturing partners use
Forge for a handoff pack.

## FAQ

**Do I have to use OhhO Build?**
It's smoothest end-to-end, but Bench also works from a manually-entered parts
list — you just fill in the wiring and ports it would otherwise infer.

**Does Bench need the real hardware?**
Yes — Bench is the step where software meets metal. The self-tests and
calibration run against the physical robot over its serial / USB buses.

## Related products

- [OhhO Build](./build.md)
- [OhhO Frame](./frame.md)
- [OhhO View](./view.md)
