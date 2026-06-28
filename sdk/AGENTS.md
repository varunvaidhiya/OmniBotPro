# OhhO OS — Engineering Handoff & Roadmap

> **Read this first.** This file is the single source of truth for continuing
> work on the OhhO OS SDK. It is written so that a fresh AI agent (or a human)
> with no prior context can understand what exists, why it's built this way, and
> exactly what to do next. Keep it up to date as milestones land.

- **Package:** `ohho-os` (imports as `ohho`) — lives in `sdk/`.
- **What it is:** the open-source, robot-agnostic engine that powers the OhhO
  platform. One API controls any robot, **with or without ROS**, and carries the
  whole stack from perception to training.
- **Status:** **M0 complete and merged** (PRs #163 brand/docs, #164 SDK core).
  Installable, 40 unittest cases passing, `ohho` CLI works end-to-end.
- **Current version:** `0.1.0` (see `ohho/__init__.py` `__version__`).

---

## 0. TL;DR — current state

| Area | State |
|---|---|
| Robot Abstraction Layer (`Robot`) | ✅ working |
| Capability model + registry | ✅ working (OmniBot, Unitree Go2, generic sim) |
| Runtime port + **native** backend | ✅ working (threaded scheduler, pub/sub, timers, params) |
| **ROS 2** runtime backend | ⛔ stub — detects rclpy, raises a clear "use native" error |
| Simulator adapter | ✅ working (holonomic physics + joint servoing) |
| **Hardware adapters** | 🟡 M1 in progress — `serial://` (Yahboom/OmniBot) + `dds://` (Unitree Go2) built and mock-tested; **on-robot bring-up pending**. DJI/others not built. Auto (no URI) still = sim; explicit `serial://`/`dds://` engages hardware. |
| Agent loop | ✅ minimal (deterministic `ScriptedBrain`); real reasoner not wired |
| Training / data / serve | ⛔ not built (extras declared in `pyproject.toml`, no code yet) |
| CLI (`ohho`) | ✅ `doctor list version connect sim drive agent` |
| Tests | ✅ 58 `unittest` cases (`sdk/tests/`) |
| CI | ✅ green (`Lint (ruff)` + repo build); see §7 |

**M1 (two-robot hardware vertical slice) is IN PROGRESS.** The Yahboom serial
(`serial://`) and Unitree DDS (`dds://`) adapters are built and unit-tested against
the protocols with mock/loopback bytes (no hardware). What remains is **on-robot
bring-up** (real OmniBot + Go2), wiring the real DDS state subscription, and the
separate OmniBot arm adapter. See §8.

---

## 1. What OhhO OS is (vision & positioning)

OhhO OS is the **engine** beneath the OhhO product family — it is **not** a
product itself and does **not** rename or absorb the products (Build, Train,
etc.). It is the open-core that those consoles run on. Open-core model:

```
   ohho.com           OhhO Cloud (the ~19 paid consoles)         ← Stripe tiers
                                 │ same APIs
   pip install →      OhhO OS  (this SDK, MIT/Apache, open)       ← you are here
```

**The four differentiators (competitor-agnostic, as advertised on `/os`):**
1. **ROS optional — never required.** Native runtime *or* ROS 2, switch with one
   argument. (Most agentic robot stacks force one or the other.)
2. **Truly robot-agnostic.** Write a behavior once; run it on a wheeled base, a
   quadruped, a humanoid, or an arm. Swapping hardware ≠ rewriting code.
3. **The whole lifecycle, one engine.** Not just control — perception, data,
   training, sim, fleet, safety.
4. **Open source, zero lock-in.**

Marketing + user docs already shipped: `website/app/os/page.tsx` and
`website/docs/ohho-os/*.md` (install, quickstart, runtimes, robots, training,
architecture). **Keep the SDK and those docs in sync.**

---

## 2. Architecture

The core idea: **the robot and the middleware are swappable backends, not forks
of your code.**

```
   your code  ·  Agent (Mind)  ·  Train / Data / Serve     ← runtime-agnostic
            │
   ┌────────┴─────────┐  Robot Abstraction Layer (RAL = ohho.robot.Robot)
   │  Runtime (port)  │  pub/sub · timers · params
   └────────┬─────────┘
      ┌──────┴───────┐
  native runtime    ros2 runtime (stub)
  (no ROS)          (rclpy + ROS 2 stack)
  asyncio/threads   Nav2 · SLAM · MoveIt 2
  direct drivers    DDS multi-machine
            │
      ┌─────┴──────┐  Adapters (one per robot family)
   sim · yahboom · unitree · dji · …   (only `sim` exists today)
```

**THE ONE INVARIANT RULE (do not break):**
> Engines (Robot, agent, future train/perception) depend **only** on the
> `Runtime` port and the `Transport` interface — they must **never import
> `rclpy`** or a specific adapter directly. This is what makes "switch runtime /
> robot with one argument" true. New backends/adapters implement the interface;
> nothing above them changes.

**Capability-typed commands.** Commands are gated by capability. A robot that
lacks a capability turns the call into a safe no-op (e.g. `move_joints` on a
robot with no arm) instead of crashing. This is what makes one behavior portable
across heterogeneous hardware.

---

## 3. Current file map (`sdk/`)

```
sdk/
├── pyproject.toml        # name=ohho-os; extras: unitree/dji/ros2/train/serve/yaml/dev/all; console_script `ohho`
├── README.md             # user-facing quickstart
├── AGENTS.md             # ← THIS FILE (handoff + roadmap)
├── .gitignore            # egg-info, __pycache__, caches
├── ohho/
│   ├── __init__.py       # exports Robot, connect, schema types, registry fns; __version__
│   ├── schema.py         # Velocity, Odometry, JointReading, Telemetry, TransportStatus, ConnectionState, clamp()
│   ├── capabilities.py   # capability string constants + ALL + is_known()
│   ├── registry.py       # RobotSpec (frozen dataclass) + _BUILTINS + get_spec/list_specs/register/load_manifest
│   ├── transport.py      # Transport (ABC) + BaseTransport (callback bookkeeping)
│   ├── runtime/
│   │   ├── __init__.py   # get_runtime("auto"|"native"|"ros2"), available_runtimes()
│   │   ├── base.py       # Runtime (ABC): pub/sub + params (concrete) + now/create_timer/start/stop (abstract); TimerHandle; RuntimeUnavailable
│   │   ├── native.py     # NativeRuntime — threaded scheduler
│   │   └── ros2.py       # Ros2Runtime — STUB (raises RuntimeUnavailable); is_available() checks rclpy
│   ├── adapters/
│   │   ├── __init__.py        # resolve_transport(uri, spec, runtime); available_adapters(); scheme map
│   │   ├── errors.py          # AdapterUnavailable
│   │   ├── sim.py             # SimTransport — holonomic physics + joint servoing; step(dt) deterministic
│   │   ├── _yahboom_proto.py  # vendored Yahboom packet codec (pure stdlib): encode + parse_stream
│   │   ├── yahboom.py         # YahboomTransport — serial:// (OmniBot base); pyserial lazy; _ingest() test seam
│   │   └── unitree.py         # UnitreeDdsTransport — dds:// (Go2); SDK lazy; velocity_to_move/sportstate_to_telemetry
│   ├── robot.py          # Robot (RAL) — connect/drive/move_joints/telemetry/emergency_stop/has/disconnect; connect() shortcut
│   ├── agent.py          # Agent + ScriptedBrain (placeholder); _default_brain() (agent_engine plug-in point)
│   ├── hardware.py       # resolve_device("auto") -> cuda/mps/cpu (lazy torch)
│   ├── cli.py            # argparse: doctor/list/version/connect/sim/drive/agent; main()
│   └── robots/
│       └── example.json  # example manifest (nested dof/limits) for load_manifest()
└── tests/                # 40 unittest cases: schema, capabilities, registry, runtime, sim, adapters, robot, agent
```

---

## 4. Public API quick reference (as built — keep stable)

```python
from ohho import Robot, connect

bot = Robot.connect("omnibot")                 # transport=None -> spec adapter, falls back to sim
bot = Robot.connect("omnibot", "sim://")       # explicit transport URI
bot = Robot.connect("omnibot", runtime="native")  # "auto" | "native" | "ros2"

bot.drive(vx=0.2, vy=0.0, w=0.3)               # clamped to spec limits; vy ignored if not holonomic; no-op if no base.drive
bot.stop()
bot.move_joints([0, -0.5, 0.5, 0, 0, 0.2])     # no-op unless capabilities include "manipulation"
bot.emergency_stop(); bot.release_stop()
t = bot.telemetry()                            # Telemetry(odom, joints, battery, custom, timestamp)
bot.has("manipulation")                        # capability check
bot.status()                                   # TransportStatus
bot.disconnect()                               # also: `with Robot.connect(...) as bot:`

from ohho.agent import Agent
Agent(bot).run("explore the room")             # returns list[str] log lines (scripted brain for now)
```

**Schema (`ohho/schema.py`):** `Velocity(linear_x, linear_y, angular_z)`,
`Odometry(x, y, theta, vx, vy, omega)`, `JointReading(name, position, velocity?)`,
`Telemetry(odom?, joints[], battery?, custom{}, timestamp)`,
`TransportStatus(protocol, state, label, latency_ms, msg_rate, error?, connected_since?)`,
`ConnectionState` enum, `clamp(value, lo, hi)`.

**RobotSpec (`ohho/registry.py`):** `id, name, category, capabilities(tuple),
adapter, dof_base, dof_arm, max_lin, max_ang, joint_names(tuple)`, `.has(cap)`.
Built-ins: `omnibot` (mecanum mobile-manip, 6-DOF arm, max_lin 0.2),
`unitree-go2` (quadruped, max_lin 1.5), `sim` (generic holonomic).

**Capabilities (`ohho/capabilities.py`):** `base.drive`, `base.holonomic_drive`,
`legged.pose`, `manipulation`, `perception.rgb`, `perception.depth`.

**Transport interface (`ohho/transport.py`):** `connect()`, `disconnect()`,
`status()`, `read()->Telemetry`, `send_velocity(Velocity)`,
`send_joint_command(name, position)`, `emergency_stop()`, `release_stop()`,
`on_telemetry(cb)->unsub`, `on_status(cb)->unsub`. Subclass `BaseTransport` (it
does the callback bookkeeping). **New adapters = subclass `BaseTransport`.**

**Runtime port (`ohho/runtime/base.py`):** `subscribe/publish` (concrete),
`get_param/set_param` (concrete), `now()`, `create_timer(period_s, cb)->TimerHandle`,
`start()`, `stop()` (abstract). **New runtimes = subclass `Runtime`.**

---

## 5. Develop / run / test

```bash
# from repo root
pip install -e sdk                       # editable, dependency-free base
# (add extras as they gain code: pip install -e 'sdk[unitree]')

python3 -m unittest discover -s sdk/tests   # 40 tests, ~1s
ohho doctor                              # environment / runtimes / adapters / robots
ohho sim --robot omnibot --seconds 5     # drive a pattern in simulation
ohho agent omnibot "explore the room"

# Lint exactly like CI (run from repo root — see §7):
pip install ruff
ruff format --check --exclude robot_ws/src/omnibot_firmware .
ruff check        --exclude robot_ws/src/omnibot_firmware .
```

---

## 6. Conventions & invariants (follow these)

- **Dependency-free `base`.** Core (`schema`, `capabilities`, `registry`,
  `transport`, `runtime/native`, `adapters/sim`, `robot`, `cli`) must import only
  the standard library. Anything heavier (torch, rclpy, cyclonedds, pymavlink,
  numpy, fastapi) goes behind an **extra** and is **imported lazily** inside the
  component that needs it (see `hardware.resolve_device` and
  `registry.load_manifest` for the pattern).
- **Style:** `from __future__ import annotations` at the top of every module;
  full type hints; docstrings on public classes/functions. **Code must pass
  `ruff format` and `ruff check`** (CI runs both repo-wide — see §7). After
  editing, run `ruff format sdk/` before committing.
- **Tests:** stdlib `unittest` (no pytest dependency), under `sdk/tests/`,
  deterministic (build `Robot` over a `SimTransport` and call `step(dt)`
  manually rather than relying on the live thread — see `tests/test_robot.py`).
  Add tests with every new module.
- **The invariant rule from §2** — engines depend only on `Runtime`/`Transport`.
- **Keep `RobotSpec` constants synced** with the real platform: OmniBot arm joint
  names mirror the ROS arm driver; OmniBot `max_lin=0.2` mirrors the Yahboom
  driver clamp. If you change one, change both and note it here.
- **Keep website docs in sync** (`website/docs/ohho-os/*`, `website/app/os`).

---

## 7. CI / git notes (important, learned the hard way)

- The repo's **`Lint (ruff)`** job (`.github/workflows/ros2_ci.yml`) runs
  `ruff format --check` **and** `ruff check` over the **whole repo** (`.`),
  config in the **root `pyproject.toml`** `[tool.ruff]`. New `sdk/` files must
  pass both. **This already bit us once** — M0's first push failed `ruff format
  --check`; fix is just `ruff format sdk/`.
- **`Build & Test (Jazzy)` `needs: lint`** — if lint fails it shows as
  *skipped*, not failed. Green lint first.
- The ROS workflow has `paths-ignore` for `**.md`, android, etc. — **Markdown-only
  changes (like editing this file) skip the ROS build entirely.**
- **Vercel** only deploys `website/**` changes (`vercel.json` ignoreCommand), so
  SDK changes don't touch the site deploy.
- **Editable-install artifacts** (`sdk/ohho_os.egg-info`, `__pycache__`) are
  gitignored via `sdk/.gitignore` — never commit them.
- **Branch:** development happened on `claude/image-analysis-6y22lz`. PRs target
  `main`. After pushing, open a **draft PR**; the repo has a PR template
  (`.github/pull_request_template.md`) — mirror its sections.

---

## 8. Roadmap (M0 → M5)

Estimates are split into **software time** (compresses well with an AI agent) and
**hardware-gated time** (needs the physical robots on a bench — does *not*
compress). M0 is done.

### ✅ M0 — Scaffold + sim slice (DONE, merged)
RAL, native runtime, sim adapter, registry, CLI, 40 tests. Acceptance: met.

### ▶ M1 — Two-robot hardware vertical slice (IN PROGRESS)
**Goal:** the *same* code drives two very different real robots, proving
robot-agnosticism. Pair: **OmniBot** (wheeled, USB serial) + **Unitree Go2**
(quadruped, DDS).

**Done so far (v0.2.0):** `serial://` Yahboom adapter (`ohho/adapters/yahboom.py`
+ vendored codec `_yahboom_proto.py`) and `dds://` Unitree adapter
(`ohho/adapters/unitree.py`), wired into `resolve_transport`, unit-tested with mock
serial bytes + pure DDS mapping functions (58 tests). Both raise a clean
`AdapterUnavailable` when their extra is absent. `[serial]` extra added.

**Remaining (hardware-gated):** on-robot bring-up against the real OmniBot + Go2;
wire the real Unitree `SportModeState` DDS subscription into `_on_state`; a separate
OmniBot **arm** adapter (Feetech bus — the base board doesn't drive the arm); an
`OHHO_HIL=1` on-hardware test harness.

**Deliverables:**
- `ohho/adapters/yahboom.py` — `YahboomTransport(BaseTransport)` over pyserial.
  **Reuse `packages/yahboom_ros2/yahboom_ros2/protocol.py`** (packet encoder/
  decoder, `packet_motion`, `packet_set_car_type`, checksum). Map `Velocity` →
  `FUNC_MOTION` (`<bhhh>` car_type, vx×1000, vy×1000, w×1000); decode RX
  `0xFB` packets → `Odometry`/`Imu`. Mecanum kinematics in
  `packages/mecanum_drive_ros2` (has a pure-Python mirror).
- `ohho/adapters/unitree.py` — `UnitreeDdsTransport(BaseTransport)` over
  `unitree_sdk2_python` (extra `[unitree]`, lazy import). **Reuse the joint-index
  maps + kp/kd defaults + topic map in `website/lib/bridge/adapters.ts`**
  (`G1_JOINT_MAP`, `UNITREE_TOPICS`, the `unitree-dds` adapter row). Map
  `Velocity` → HighCmd; LowState/odometry → `Telemetry`.
- Register schemes in `adapters/__init__.py` (`AVAILABLE += ("yahboom","unitree")`;
  parse `serial://`, `dds://`); update `available_adapters()` and `ohho doctor`.
- **HIL test harness:** `sdk/tests/hil/` (skipped unless `OHHO_HIL=1`) that runs
  connect→drive→read against real hardware; plus a **loopback/mock test** (feed
  canned serial/DDS bytes) that runs in CI with no hardware.
- Extras: real deps in `pyproject.toml` `[unitree]`/serial.
**Acceptance:** `Robot.connect("omnibot","serial:///dev/ttyUSB0")` and
`Robot.connect("unitree-go2","dds://<ip>")` both drive + stream telemetry on
hardware, and `Agent(bot).run(goal)` runs unchanged on both. Mock-byte tests
pass in CI.
**Time:** adapter code ~software; final bring-up **hardware-gated**.
> If hardware isn't on hand: build both adapters against the protocols with
> mock/loopback tests now; only the on-robot bring-up waits for the bench.

### M2 — Real agent brain
Wire `agent.py` `_default_brain()` to **`agent_engine`**
(`agent_engine/reasoners/claude_tool_caller.py` + `reasoning/router.py`); build a
`ToolRegistry` from the robot's capabilities; keep `ScriptedBrain` as the
no-dependency fallback. Acceptance: `Agent(bot).run(goal)` performs real
perceive→reason→act→reflect when `[agent]` extra + `ANTHROPIC_API_KEY` present.

### M3 — Training & data pipelines
`ohho/data/` (record → LeRobot dataset; reuse `data_engine/` +
`packages/robot_episode_dataset`), `ohho/train/` (delegate to `learning_engine/`
+ `lerobot_engine/`; `device="auto"` via `hardware.resolve_device`), `ohho/serve/`
(wrap `packages/vla_serve` FastAPI). Acceptance: record→train→serve loop on the
sim robot; matches `website/docs/ohho-os/training.md`.

### M4 — ROS 2 runtime backend
Implement `Ros2Runtime` (rclpy) behind the existing `Runtime` port: timers via
rclpy timers, pub/sub via topics, params via node params. Add `ros2://`
transport that bridges to the `robot_ws/` stack (Nav2/SLAM/MoveIt). Acceptance:
`runtime="ros2"` runs the identical Robot/agent/train code with **zero changes
above the port**. Flip `get_runtime("auto")` to prefer ros2 when rclpy present.

### M5 — Packaging, profiles & community
PyPI publish (`ohho-os`), `curl | sh` installer behind `ohho.com/install.sh`,
hardware profiles (reuse `learning_engine/hardware/profiles.py` +
`onnx_providers`), `ohho market` skill registry, `CONTRIBUTING.md`
("add a robot = one manifest + one adapter + one test"). Update root `CLAUDE.md`
to document the `sdk/` area.

---

## 9. START HERE: concrete first steps for M1

1. `pip install -e sdk` and run the tests to confirm a green baseline.
2. Read `packages/yahboom_ros2/yahboom_ros2/protocol.py` and
   `website/lib/bridge/adapters.ts` (Unitree maps). Read `confirmed_protocol.py`
   at repo root for the Yahboom serial reference.
3. Create `sdk/ohho/adapters/yahboom.py`:
   - `class YahboomTransport(BaseTransport): protocol = "serial"`.
   - Lazy `import serial` (pyserial). Constructor takes `(spec, port, baud=115200)`.
   - `connect()` opens the port, sends `packet_set_car_type(X3)`; `send_velocity`
     encodes `FUNC_MOTION`; a reader thread decodes `0xFB` packets → cache
     `Telemetry`; `read()` returns the cache; `emergency_stop()` sends zero.
   - Honor the invariant: no ROS imports.
4. Wire `serial://` and a `--port` into `adapters/__init__.py` + `cli.py`.
5. Add `sdk/tests/test_yahboom_mock.py` — feed canned bytes through the
   decoder, assert odometry; no hardware needed (runs in CI).
6. Repeat for `unitree.py` (`dds://`, extra `[unitree]`, mock LowState bytes).
7. `ruff format sdk/`, run unittest, then commit on a feature branch and open a
   **draft PR** to `main`. Update §0/§3/§8 of this file as you land each piece.

---

## 10. Decisions made (don't re-litigate without reason)

- **Name:** "OhhO OS" is the **engine**, sits beside the products, renames
  nothing. Confirmed by the user.
- **Runtime posture:** **both** ROS and no-ROS, user chooses; the chooser
  explains trade-offs (shipped on `/os` + `docs/ohho-os/runtimes.md`).
- **Launch robots:** OmniBot + Unitree Go2 (max contrast: wheeled/serial/has-arm
  vs legged/DDS/no-arm).
- **Language:** Python-first; the web `Transport` types are the shared contract.
- **License:** `pyproject.toml` declares **Apache-2.0** (placeholder; user to
  confirm MIT vs Apache-2.0 — both were offered). No `LICENSE` file yet → add one
  in M5 (or when the user confirms).

### Open questions for the user
- MIT vs Apache-2.0 for the open core (affects the `LICENSE` file + `pyproject`).
- Is the dedicated public OSS repo `varunvaidhiya/OmniBotPro`, or a separate
  clean `ohho-os` repo for launch? (Website `GITHUB_HREF` currently points at
  OmniBotPro.)

---

## 11. Gotchas

- `get_runtime("auto")` returns **native** in M0 (ros2 backend unimplemented);
  flip this in M4.
- `Robot.connect("<any>")` with no transport **silently runs in simulation**
  today (auto fallback). An **explicit** non-sim URI raises `AdapterUnavailable`
  — that's intentional (honest error vs convenient default).
- The sim's `start_sim()` runs a background thread; for deterministic tests
  construct `Robot(spec, SimTransport(spec), NativeRuntime())` directly and call
  `transport.step(dt)` (don't start the thread).
- `ohho_os.egg-info` / `__pycache__` are gitignored — if `git add sdk` ever tries
  to stage them, the `.gitignore` is missing.

---

## 12. References

- **Website surfaces:** `website/app/os/page.tsx` (marketing),
  `website/docs/ohho-os/{README,install,quickstart,runtimes,robots,training,architecture}.md`.
- **Transport contract (TS source):** `website/lib/connect/types.ts`;
  **adapter catalog / joint maps:** `website/lib/bridge/adapters.ts`;
  **robot taxonomy:** `website/lib/garage/types.ts`.
- **Reuse targets:** `packages/yahboom_ros2`, `packages/mecanum_drive_ros2`,
  `agent_engine/`, `learning_engine/`, `lerobot_engine/`, `vla_engine/`,
  `packages/vla_serve`, `data_engine/`, `robot_ws/` (ROS backend).
- **Merged PRs:** #163 (brand + docs), #164 (SDK M0 core).
- **Repo guide:** root `CLAUDE.md` (monorepo conventions, physical constants,
  topic map).

---

_Last updated: M1 adapters landed (SDK `v0.2.0`). When you finish a milestone,
update §0, §3, and §8, and bump `ohho/__init__.py` `__version__`._
