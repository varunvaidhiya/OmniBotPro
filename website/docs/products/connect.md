# OhhO Connect

**One link. Any robot. Any transport.**

- **Category:** Foundation
- **Accent:** cyan
- **Live app:** [Open the garage](/garage)

A robot-agnostic connection layer that bridges your browser to any robot over
Wi-Fi, USB, Bluetooth or a built-in simulator — so every OhhO console works with
any hardware, no install required.

## Overview (hero)

One link to any robot. OhhO Connect is the transport abstraction that lets every
OhhO console — Pilot, Autonomy, Fleet, Mind — talk to any robot over whatever
link the hardware exposes: Wi-Fi via ROSBridge, USB via Web Serial, Bluetooth Low
Energy, or a built-in simulator. No SDK install, no Python, no robot-specific
setup.

## Highlights

- Wi-Fi · ROSBridge WebSocket
- USB · Web Serial (firmware-direct)
- Bluetooth · BLE (Nordic UART)
- MQTT · IoT & cloud fleet connectivity
- Built-in simulator (no hardware)
- Robot-agnostic — one console, any robot

## What you get

- Every robot speaks a different language on a different wire. A DDS-native
  humanoid talks DDS over Cyclone, a mecanum base speaks a serial protocol over
  USB, a drone speaks MAVLink, an industrial arm speaks Modbus. OhhO Connect is
  the layer that makes all of them look the same to every OhhO product.
- Connect exposes a single transport interface — connect, send velocity, send
  joint commands, emergency stop, subscribe to telemetry — and implements it for
  each protocol. A console built against Connect works on a mecanum manipulator
  over Wi-Fi today and a humanoid over USB tomorrow, with zero code changes.
- Because Connect runs in the browser, there's nothing to install on the
  operator's machine. Web Serial and Web Bluetooth are feature-detected at
  runtime, and a deterministic simulator is always available — so you can explore
  every console before you ever wire up real hardware.

## Features

- **Five transports, one API** — ROSBridge WebSocket (Wi-Fi), Web Serial (USB),
  Web Bluetooth (BLE), MQTT (IoT/cloud fleet), and a built-in simulator — all
  behind the same RobotTransport interface, feature-detected at runtime.
- **Robot-agnostic by design** — Connect speaks generic velocity and joint
  commands, not robot-specific SDK calls. A console built against Connect works
  on any robot with a transport implementation.
- **Browser-native, no install** — Web Serial and Web Bluetooth run in Chromium
  browsers over a secure context — no driver install, no Python SDK, no desktop
  app. Open a URL and drive.
- **Mixed-content aware** — Connect detects the HTTPS-to-ws mismatch and guides
  the operator to a secure rosbridge or a LAN connection, so the link just works
  instead of failing silently.
- **Persistent per-robot config** — Each robot in the garage remembers its last
  protocol, address and baud rate — so reconnecting is one click, not a setup
  wizard every time.
- **Simulator always on** — A deterministic in-browser robot streams telemetry on
  every protocol slot, so you can demo, develop and test consoles without
  hardware on the bench.

## How it works

1. **Pick a robot** — Select a robot from your OhhO Garage — Connect loads its
   saved transport settings.
2. **Choose a protocol** — Connect shows the protocols available in your browser
   and on your robot — Wi-Fi, USB, BLE or Sim.
3. **Open the link** — Connect establishes the transport, runs capability checks
   and streams live telemetry to every console.
4. **Operate** — Every OhhO console reads the live connection and never needs to
   know which protocol is underneath.

## Specs

| Spec | Value |
|---|---|
| Transports | ROSBridge (WS), Web Serial (USB), Web BLE, MQTT, Simulator |
| Interface | RobotTransport — connect, velocity, joints, e-stop, telemetry |
| Browser | Chromium (Chrome / Edge) for Serial + BLE; any for WS |
| Persistence | Per-robot config saved to garage (Supabase + localStorage) |
| Simulator | Deterministic in-browser telemetry stream |
| Integrates | OhhO Pilot, Autonomy, Fleet, Mind, all consoles |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Included — Wi-Fi + simulator | ✅ |
| Builder | Included + USB + BLE | ✅ |
| Fleet | Included + multi-robot sessions | ✅ |
| Forge | Included + custom transports | ✅ |

**Recommended plan: Spark.** Connect is foundational and included on every plan,
including free. You only need a paid plan for the consoles that sit on top of
Connect — Pilot, Autonomy, Fleet and Mind.

## FAQ

**Do I need to install anything on my computer?**
No. Connect runs entirely in the browser. Web Serial and Web Bluetooth are built
into Chromium browsers (Chrome, Edge) — no driver, no SDK, no desktop app.

**Does Connect work with non-ROS robots?**
Yes. Web Serial talks the firmware protocol directly — no ROS needed. For
DDS-native robots, pair Connect with OhhO Bridge to translate between DDS and
ROS topics.

**What if my browser doesn't support Web Serial?**
Connect feature-detects each protocol at runtime and shows only the ones your
browser supports. Wi-Fi (ROSBridge) and the simulator work in any modern browser.

## Related products

- [OhhO Bridge](./bridge.md)
- [OhhO Frame](./frame.md)
- [OhhO Pilot](./pilot.md)
