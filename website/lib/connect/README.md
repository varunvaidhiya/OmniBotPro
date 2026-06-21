# OhhO Connect — robot connection layer

A robot-agnostic way to open a **live link** to any robot in the garage, from
any console, over whatever the hardware exposes. One robot is connected at a
time; the connection survives navigation between consoles.

## How it fits together

```
app/layout.tsx
  └─ <RobotConnectionProvider>          ← the ONE live connection (survives nav)
       ├─ <ConnectionBar/>              ← floating "connected robot" HUD (top of every console)
       └─ {children}                    ← consoles call useRobotConnection()

Garage card → <ConnectButton> → <ConnectModal> → connect(robot, cfg)
                                                     │
                                          createTransport(robotConfig, cfg)
                                                     │
        ┌────────────────────────────────┬─────────┴───────────┬──────────────┐
   RosbridgeTransport            WebSerialTransport     WebBluetoothTransport  SimulatedTransport
   (Wi-Fi, ws://…:9090)          (USB, Yahboom/JSON)    (BLE Nordic UART)      (no hardware)
```

Every transport implements the same `RobotTransport` interface (`types.ts`):
`connect / disconnect / sendVelocity / sendJointCommand / emergencyStop /
releaseStop / onStatus / onTelemetry`. Consoles never touch a protocol directly —
they call `useRobotConnection()`.

## Protocols

| id | Transport | When to use |
|---|---|---|
| `rosbridge` ⭐ | `WebSocket` → rosbridge v2 JSON (port 9090) | **Default.** Same path the Android app uses. Any ROS 2 robot. |
| `webserial` | `navigator.serial` | USB cable, no ROS/network. OmniBot → Yahboom protocol; others → JSON lines. |
| `webbluetooth` | `navigator.bluetooth` | BLE / Nordic UART. Experimental, needs robot firmware support. |
| `simulated` | in-browser | Demo every console with no hardware. |

Topic names for `rosbridge` come from the robot's `RobotConfig.rosTopics`, so the
transport adapts per robot automatically.

## Per-robot settings

Saved in `UserRobot.config.connection` (the existing JSONB column — **no DB
migration**). Round-trips through the garage client (Supabase + localStorage
fallback) via `config.ts`.

## Deployment note (HTTPS ↔ ws://)

A page served over **https://** cannot open an insecure **ws://** socket
(browser mixed-content policy). For a remote robot use **wss://** (TLS rosbridge),
or open the site via `http://localhost` on the same LAN as the robot. The
ConnectModal surfaces this warning automatically (`rosbridgeMixedContentWarning`).
USB and Bluetooth are unaffected. Web Serial / Web Bluetooth are Chromium-only
and require a secure context (https or localhost).

## Extending

- **Add a protocol:** implement `RobotTransport` in a new file, add a row to
  `PROTOCOLS` + `isProtocolAvailable()` in `protocols.ts`, and one `case` in
  `factory.ts`. The modal and bar pick it up automatically.
- **Tune a robot's defaults:** the rosbridge address/topics derive from
  `RobotConfig`; pin specifics in `FLAGSHIP_OVERRIDES` (lib/garage/robot-config.ts).
- **New firmware protocol over USB/BLE:** branch in `WebSerialTransport` /
  `WebBluetoothTransport` (see `usesYahboom()`), reusing `yahboom.ts` as a model.
```
