# Contributing to OhhO OS

OhhO OS is the open-source, robot-agnostic engine that powers the OhhO
platform. We welcome contributions of all sizes — from bug fixes and docs to
new robot adapters and skills.

## The one-rule contribution model

> **Add a robot = one manifest + one adapter + one test.**

That's it. Everything else (the agent, training, serving, CLI) works
automatically because it targets the `Robot` abstraction and the `Runtime` /
`Transport` ports, not your specific hardware.

---

## Getting started

```bash
pip install -e sdk                      # editable, dependency-free base
python -m unittest discover -s sdk/tests   # should be green
ohho doctor                             # check your environment
```

## Adding a robot

### 1. Write a manifest (JSON or YAML)

```json
{
  "id": "my-bot",
  "name": "My Bot",
  "category": "wheeled",
  "capabilities": ["base.drive", "perception.rgb"],
  "adapter": "my-adapter",
  "dof_base": 3,
  "dof_arm": 0,
  "max_lin": 0.5,
  "max_ang": 1.0
}
```

### 2. Write an adapter (subclass `BaseTransport`)

```python
# sdk/ohho/adapters/my_adapter.py
from .transport import BaseTransport

class MyAdapterTransport(BaseTransport):
    protocol = "my-adapter"

    def connect(self):
        # open your hardware link
        ...

    def send_velocity(self, vel):
        # send vel.linear_x, vel.linear_y, vel.angular_z to your hardware
        ...

    def read(self):
        # return a Telemetry snapshot
        ...

    # ... implement the rest of the Transport interface
```

### 3. Register the scheme

Add your adapter to `sdk/ohho/adapters/__init__.py`:

```python
from .my_adapter import MyAdapterTransport
_HARDWARE["my-adapter"] = MyAdapterTransport
```

### 4. Write a test (no hardware needed)

```python
# sdk/tests/test_my_adapter.py
import unittest

class TestMyAdapter(unittest.TestCase):
    def test_connect_and_drive(self):
        # use a fake serial/bus, assert the right bytes were sent
        ...
```

### 5. Run lint + tests

```bash
ruff format sdk/
ruff check sdk/
python -m unittest discover -s sdk/tests
```

---

## Adding a skill

A skill is a reusable behavior registered with `@skill`:

```python
from ohho.market import skill

@skill("dance", "Make the robot dance", requires=["base.drive"])
def dance(robot, speed=0.2):
    robot.drive(vx=speed, w=0.8)
    # ...
    robot.stop()
    return "dance complete"
```

Users discover and run it via:

```bash
ohho market list
ohho market run omnibot dance
```

---

## Architecture invariants (do not break)

1. **Engines depend only on `Runtime` + `Transport`.** Never import `rclpy`,
   `serial`, `lerobot`, or a specific adapter from `ohho.robot`, `ohho.agent`,
   `ohho.brains`, `ohho.data`, `ohho.train`, or `ohho.serve`. This is what
   makes "switch runtime / robot with one argument" true.

2. **Dependency-free base.** Core modules (`schema`, `capabilities`,
   `registry`, `transport`, `runtime/native`, `adapters/sim`, `robot`, `cli`)
   import only the standard library. Heavy deps (torch, rclpy, pyserial,
   lerobot, pyarrow, fastapi, anthropic) go behind an **extra** and are
   **imported lazily** inside the component that needs them.

3. **Capability-typed commands.** A robot that lacks a capability turns the
   call into a safe no-op (e.g. `move_joints` on a base with no arm) instead of
   crashing. This is what makes one behavior portable across heterogeneous
   hardware.

4. **Tests are stdlib `unittest`.** No pytest dependency. Deterministic — build
   a `Robot` over a `SimTransport` and call `step(dt)` manually rather than
   relying on live threads. Add tests with every new module.

5. **`ruff format` + `ruff check` must pass.** CI runs both repo-wide. Run
   `ruff format sdk/` before committing.

---

## Style

- `from __future__ import annotations` at the top of every module.
- Full type hints. Docstrings on public classes/functions.
- No comments unless asked. Code should be self-documenting.

## Pull request checklist

- [ ] Tests pass: `python -m unittest discover -s sdk/tests`
- [ ] Lint passes: `ruff format --check sdk/ && ruff check sdk/`
- [ ] `AGENTS.md` §0/§3 updated if you added files or changed the state table
- [ ] Version bumped in `ohho/__init__.py` and `pyproject.toml` for milestones

## License

Apache-2.0. See `sdk/LICENSE`.
