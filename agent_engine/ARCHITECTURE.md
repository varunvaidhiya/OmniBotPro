# Agent Engine — Architecture

The agent engine is OmniBot's **deliberative brain**: a continuous
perceive → reason → verify → act → monitor → reflect → remember loop layered on
top of the existing reflexive control stack (ROS drivers, muxes, RL/VLA policies
at 10–20 Hz). It turns a one-shot command executor into a continuously-operating
physical agent.

Design follows `learning_engine`'s convention: a **pure-Python core**
(`agent_engine.core`, numpy-only, fully unit-testable) with **adapters at the
edges** (ROS, cloud Claude, learning-engine reuse) imported lazily.

## The loop (`core/harness.py`)

`AgentHarness` runs a small state machine, one transition per `tick()` so a ROS
timer or a test can drive it:

```
IDLE ──goal──► PERCEIVE ──► PLAN ──► ACT ──► MONITOR ──┐
  ▲                            │                       │
  │                            ├─ goal_complete/give_up┤
  │                            ├─ ask_human ─► WAIT_HUMAN
  │                            │                       │ (budget left)
  └── REMEMBER ◄── REFLECT ◄───┴──────────◄────────────┘ loop to PERCEIVE
```

`MONITOR → PERCEIVE` is the closed loop: the agent re-grounds in fresh state
each cycle. A per-goal step budget bounds it; the reasoner ends it via
`goal_complete`. E-stop and `ask_human` are first-class interrupts.

## Ports (`core/interfaces.py`)

The harness is hexagonal — it depends only on structural `Protocol` ports, never
on ROS/anthropic/learning_engine directly:

| Port | Adapter (where) |
|---|---|
| `Perceptor` | `world_state_node` → `/agent/world_state` (ROS); stubs in tests/sim |
| `Reasoner` | `ClaudeToolCallingReasoner` (JSON tool-calling via the router); `ScriptedReasoner` for tests |
| `MemoryPort` | `WorkingMemory` over `EntityMemory` + episode store |
| `Reflector` | learning-engine `ReflectionEvaluator` / `LanguageGoalEvaluator` |
| `VerifierPort` | learning-engine `InferenceVerifier` (hardware-safety gate) |
| `ReasoningBackend` | cloud Claude / on-device LLM / DeepX NPU (via `ReasoningRouter`) |
| `EntityStore` | `omnibot_orchestration` `EntityMemory` (duck-typed) |

## Components

- **`core/blackboard.py` — `WorldState`**: one fused snapshot (base pose/vel, arm
  joints, detected objects, mission phase, scene text, memory summary, e-stop).
  `to_observation()` emits the 9-D `{"state": …}` dict the safety verifier needs;
  `to_dict()`/`from_dict()` round-trip the `/agent/world_state` topic.
- **`core/tools.py` — `ToolRegistry`**: the agent's actuators as callable tools
  with Claude tool-use schemas. `low_level=True` tools must pass the verifier.
- **`reasoning/router.py` — `ReasoningRouter`**: hybrid brain. Picks the first
  *available* backend per call kind (`deliberate` → cloud, `reactive` →
  on-device), degrading gracefully offline. Backends: `CloudClaudeBackend`
  (`claude-sonnet-4-6`), `LocalLLMBackend` (Ollama/llama.cpp/DeepX-compiled),
  `EchoBackend` (offline/test stub).
- **`memory/working_memory.py` — `WorkingMemory`**: injects long-term
  (`EntityMemory`) + short-term (recent outcomes) memory into every prompt and
  writes grounded objects back. Fixes the "memory exists but unused" gap.

## Reuse (not rebuilt) — `integrations/learning_engine.py`

The reflective and safety machinery already exists in `learning_engine` and is
wired in via lazy adapters:

- `WorldStateVerifier` → `VerifierPort` over `SafetyCheck`/`ReachabilityCheck`
  (real Yahboom/Feetech limits).
- `EvaluatorReflector` → `Reflector` over the `LanguageGoalEvaluator` →
  `ReflectionEvaluator` → `HeuristicSelfEvaluator` chain; labels and persists
  the episode to a `ReplayDataset`.
- `ReplayMemorySource` → recent labelled-episode summaries for the prompt.
- `ContinualLearningClosure` → fires `ContinualLearningScheduler` (reflect →
  learn). Hardware/EP selection (incl. the DeepX NPU profile) comes from
  `learning_engine.hardware`.

## ROS edge (in `omnibot_orchestration`)

- `world_state_node.py` — fuses `/odom`, `/arm/joint_states`,
  `/perception/object_info`, `/mission/status`, scene description, entity memory
  → `/agent/world_state`.
- `agent_node.py` — runs `AgentHarness.tick()` from a timer; subscribes
  `/ai/command` + `/agent/goal`, `/emergency_stop`; tools publish
  `/mission/command`, `/control_mode`, `/arm/cmd_mode`, `/mission/cancel`,
  `/ai/response_needed`.

## Testing

`python3 -m unittest discover -s agent_engine/tests -t .` — stdlib unittest, no
ROS/torch/anthropic. Covers blackboard, tools, router, working memory, and the
full loop with stub ports.
