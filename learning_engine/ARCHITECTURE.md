# OmniBot Learning Engine — Architecture

A modular post-training and continual-learning framework. The robot improves
from demonstrations, simulation experience, real-world execution,
self-evaluation and reward feedback. The design is **architecture-first**:
every layer is a pluggable component behind an interface; no layer depends on
a specific algorithm or foundation model.

---

## 1. System architecture

```
                         ┌────────────────────────────────────────────────┐
                         │              POST-TRAINING LOOP                │
                         │   (loop/post_training_loop.py — orchestration  │
                         │    only; every box below is injected)          │
                         └────────────────────────────────────────────────┘

  DATA COLLECTION                REWARDS & EVALUATION             LEARNING
┌──────────────────┐   Episode ┌───────────────────────┐   ┌────────────────────┐
│ teleop demos     │──────────►│ RewardEngine          │   │ PolicyTrainer      │
│ human demos      │           │  task / dense / safety │   │  behavior_cloning  │
│ sim rollouts     │           │  efficiency/smoothness │   │  offline_rl_awr    │
│ real exec logs   │           ├───────────────────────┤   │  online_rl (Isaac) │
│ rosbags          │           │ VLMRewardModel        │──►│  finetune_smolvla  │
└────────┬─────────┘           │ LanguageGoalEvaluator │   └─────────┬──────────┘
         │                     │ Reflection/Heuristic  │             │ checkpoint
         ▼                     └──────────┬────────────┘             ▼
┌──────────────────┐  outcome + score     │            ┌────────────────────┐
│ ReplayDataset    │◄─────────────────────┘            │ PolicyVersionMgr   │
│ (on-disk, npz)   │                                   │ (rollback registry)│
├──────────────────┤                                   └─────────┬──────────┘
│ EpisodicReplay   │── stratified success/near/failure ──► train │ deploy
│ Prioritized PER  │                                             ▼
└──────────────────┘                                   ┌────────────────────┐
                                                       │ Policy (interface) │
  SIMULATION                                           │  SmolVLA / OpenVLA │
┌──────────────────┐                                   │  ONNX RL / MLP ... │
│ Isaac Lab        │                                   └─────────┬──────────┘
│ MuJoCo/ManiSkill │   SimRolloutCollector                       │
│ Gazebo (HIL)     │◄── curriculum + domain rand                 ▼
└──────────────────┘                                   ┌────────────────────┐
                                                       │ InferenceVerifier  │
  CONTINUAL LEARNING                                   │  Best-of-N + safety│
┌────────────────────────────┐                         │  + reachability    │
│ ContinualLearningScheduler │ triggers: new episodes, │  + task likelihood │
│ (watches ReplayDataset)    │ new tasks, staleness    └─────────┬──────────┘
└────────────────────────────┘                                   ▼
                                                            robot (ROS 2)
```

Dependency rule: every layer depends only on `core/` (types + interfaces +
registries). Concrete implementations register by name so YAML configs can
assemble the whole system (`configs/learning_loop.yaml`).

Hardware split (matches the existing OmniBot deployment):

| Where | What runs |
|---|---|
| Raspberry Pi 5 | `episode_logger_node` (records execution into replay format) |
| GPU desktop | learning loop, trainers, VLM judges, sim backends, verification |
| Both | `ReplayDataset` directories synced Pi → desktop (rsync/NFS) |

## 2. Folder structure

```
learning_engine/
├── core/                 # types.py, interfaces.py, registry.py, config.py
├── data/                 # schema.py, replay_dataset.py, collectors.py
├── sim/                  # adapters.py (IsaacLab/MuJoCo/ManiSkill/Gazebo), curriculum.py
├── rewards/              # engine.py, terms.py, vision.py
├── evaluation/           # judge.py (VLM clients), language_goal.py, self_eval.py
├── replay/               # buffer.py (uniform, PER, episodic store)
├── policies/             # base.py, trainers.py, adapters.py (foundation models)
├── verification/         # verifier.py (Best-of-N + checks)
├── loop/                 # post_training_loop.py, continual.py
├── hardware/             # device.py (auto device + ONNX providers), profiles.py
├── benchmarks/           # system_probe, monitors, ai_benchmark, reporters, run.py
├── ros2/                 # topics.py, episode_logger_node.py
├── configs/              # learning_loop.yaml (example full config)
└── tests/                # unittest suite (numpy-only, no optional deps)
```

Layers 1–9 are covered in §3–§9; portability and benchmarking in §10.

## 3. Component interfaces (`core/interfaces.py`)

| Interface | Key method | Implementations |
|---|---|---|
| `DataCollector` | `collect() -> Iterable[Episode]` | sim_rollout, execution_log, teleop_dataset, rosbag |
| `SimulationEnv` | `reset()/step(a) -> (obs, r, done, info)` + `set_goal`, `randomize` | isaac_lab, mujoco, maniskill, gazebo |
| `RewardTerm` | `compute(transition, context) -> float`, `reset()` | 9 built-ins (see §8) |
| `RewardModel` | `score(instruction, frames) -> [0,1]` | vlm, learned |
| `VLMClient` | `complete(prompt, images) -> str` | ClaudeVLMClient, StaticVLMClient |
| `EpisodeEvaluator` | `evaluate(episode) -> EvaluationReport` | language_goal, reflection, heuristic_self_eval |
| `ReplayBuffer` | `add / sample / __len__` | uniform, prioritized (PER) |
| `Policy` | `predict(obs, task)`, `sample_plans(...)` | mlp, onnx, random, zero, smolvla, openvla, vla_serve |
| `PolicyTrainer` | `train(dataset, policy) -> TrainResult` | behavior_cloning, offline_rl_awr, online_rl, finetune_smolvla, noop |
| `PlanCheck` | `check(plan, obs, ctx) -> (score, reason)` | safety (hard), reachability (hard), task_likelihood (soft) |

Adding a component = implement the interface + `@REGISTRY.register("name")`.
Nothing else changes.

## 4. Data schemas (`data/schema.py`)

**Observation** — `Dict[str, np.ndarray]`:

| Key | Shape | Source topic |
|---|---|---|
| `state` | (9,) = arm ×6 + base vel ×3 | `/arm/joint_states` + `/odom` |
| `images.front` | (480, 640, 3) bgr8 | `/camera/front/image_raw` |
| `images.wrist` | (240, 320, 3) bgr8 | `/camera/wrist/image_raw` |
| `images.bev` | (H, W, 3) | `/camera/base/bev/image_raw` |
| `lidar_sectors` | (8,) | synthesized from depth (rl_nav convention) |
| `goal` | (3,) | goal-conditioned tasks |

**Action** — (9,) = arm joint positions ×6 + base (vx, vy, ωz), matching
`MOBILE_MANIP_ACTION_SPEC` in `data_engine/schema/constants.py`. Base-only
policies use (3,).

**Episode storage** (ReplayDataset): `episodes/<id>/steps.npz` (time-stacked
arrays) + `meta.json` (instruction, source, outcome, success_score, per-step
reward breakdowns, evaluation reports) + an `index.jsonl` for filtering.
`RewardBreakdown` stores raw term values *and* weights so rewards can be
re-weighted offline without recollecting.

Outcome labels: `success / near_success / failure / aborted / unknown`
(thresholds: confidence ≥ 0.8 success, ≥ 0.5 near-success).
Sources: `teleop / human_demo / simulation / real_execution / augmented`.

LeRobot interop: `schema.LEROBOT_KEY_MAP` maps keys to
`observation.state` / `observation.images.*` / `action` for VLA fine-tuning
datasets.

## 5. ROS 2 integration points (`ros2/`)

The learning engine does **not** add nodes to robot_ws; it integrates at the
edges:

1. **`episode_logger_node`** (run on the Pi):
   subscribes to the *post-mux* command topics (`/cmd_vel/out`,
   `/arm/joint_commands/out` — i.e. what the hardware actually executed),
   `/odom`, `/arm/joint_states`, all cameras, `/emergency_stop` and
   `/mission/status`. Segments episodes on `/learning/episode/start|stop`
   (String) or automatically on mission-status transitions; writes
   ReplayDataset-format episodes that `ExecutionLogCollector` ingests.
2. **Policy deployment** is unchanged: trainers emit ONNX (consumed by the
   existing `omnibot_rl` nodes) or SmolVLA checkpoints (consumed by
   `policy_node`), so the deployment path is the one already in production.
3. **`VlaServePolicy`** evaluates whatever model the `vla_serve` FastAPI
   server is serving, keeping eval and deployment on the same weights.
4. New topics are namespaced under `/learning/*` (see `ros2/topics.py`);
   all other names mirror the CLAUDE.md topic map.

## 6. Training pipeline

```
ContinualLearningScheduler.step()
  └─ PostTrainingLoop.run_iteration()
       1. COLLECT      every DataCollector (sim rollouts, ingested real logs, demos)
       2. REWARD       RewardEngine.annotate_episode → per-step RewardBreakdown
       3. EVALUATE     evaluator chain (language_goal → reflection → heuristic
                       fallback) → outcome + completion confidence + analyses
       4. STORE        ReplayDataset.add_episode (evaluation persisted in meta)
       5. TRAIN        EpisodicReplayStore stratified sample → PolicyTrainer
       6. EVAL POLICY  held-out rollouts → success rate
       7. REPORT       IterationReport JSON (reports feed W&B / Grafana later)
```

Failure isolation: storage happens before training; a judge outage degrades
to the heuristic evaluator; one failing collector never aborts the iteration.

Trainer strategy selection is config-only (`trainer.name`). Heavy trainers
delegate to the existing stacks rather than duplicating them: `online_rl` →
`rl_engine/scripts/train_*.py` (Isaac Lab PPO + ONNX export), and
`finetune_smolvla` → `lerobot_engine/train.py`.

## 7. Inference pipeline

```
observation ──► Policy.sample_plans(obs, task, n=4, horizon=10)
                    │  N candidate action sequences
                    ▼
            InferenceVerifier
              hard checks:  SafetyCheck (0.2 m/s clamp, ±0.05 rad/step arm
                            delta, URDF joint limits — the real Yahboom/
                            Feetech limits, so verified ⇒ executable)
                            ReachabilityCheck (displacement envelope, goal
                            improvement)
              soft checks:  TaskLikelihoodCheck (RewardModel on current frame)
                            consistency bonus (agreement with candidate
                            majority — self-consistency)
                    │  argmax aggregate score
                    ▼
            VerificationResult.action  (first action, receding horizon)
            └─ fallback: ZeroPolicy when every candidate is rejected
```

## 8. Reward engine design

`RewardEngine([RewardTerm, ...])` — weighted multi-objective composition;
each step gets a full `RewardBreakdown` (vector + weighted scalar).

| Group | Terms (registry names) |
|---|---|
| Task | `task_success` (sparse ±bonus) |
| Dense | `goal_distance`, `goal_progress` (potential-based shaping) |
| Safety | `collision` (info flag or lidar < threshold), `joint_limit` |
| Efficiency | `energy` (−‖a‖²), `time` (per-step cost) |
| Smoothness | `action_smoothness` (−‖Δa‖²), `trajectory_smoothness` (state jerk) |

Stateful terms (progress, smoothness) reset at episode boundaries.
Vision rewards (`rewards/vision.py`): `VLMRewardModel` does zero-shot visual
task verification on sampled frames (instruction + image/video → completion
score); its labels are the training data for `LearnedRewardModel`, a
distilled per-frame model fast enough for dense scoring at policy frequency.

## 9. Replay buffer design

Three tiers (`replay/buffer.py`):

1. **`ReplayDataset`** — durable, on-disk, append-only; survives restarts;
   the source of truth.
2. **`EpisodicReplayStore`** — episode-level prioritization over the
   dataset: outcome-stratified sampling (default 50 % success / 30 %
   near-success / 20 % failure) with exponential recency bias; feeds
   trainers and fills transition buffers with success-score priorities.
3. **`PrioritizedReplayBuffer`** — in-memory proportional PER
   (P(i) ∝ pᵢ^α, IS weights (N·P)^−β, `update_priorities` for TD errors)
   for online/offline RL inner loops. `UniformReplayBuffer` for BC.

## 10. Hardware portability & benchmarking

One codebase, many compute targets. The rule: **hardware knowledge lives
only in `hardware/`**; everything else asks for `device="auto"` or
`onnx_providers()` and stays portable. `hardware/device.py` detects the
accelerator and resolves devices + ONNX providers; `hardware/profiles.py`
names the deployment topologies. `benchmarks/` runs the same suites on any
target and publishes the results (see §2 for the file map).

**Supported targets** (via `hardware.profiles.BUILTIN_PROFILES`):

| Profile | Topology | Inference path |
|---|---|---|
| `pi_workstation` | Pi 5 + NVIDIA workstation (current production) | CUDA |
| `pi_accelerator_workstation` | Pi 5 + Hailo/Coral + workstation | HEF/EdgeTPU + CUDA |
| `jetson_single` | one Jetson Orin runs everything | TensorRT/CUDA |
| `workstation_single` | one GPU box (sim development) | CUDA |
| `mac_dev` | Apple M-series (MPS/CoreML, no Isaac) | MPS / CoreML |

`OMNIBOT_HW_PROFILE` env var forces a profile; otherwise `detect_profile()`
guesses from the hardware. New targets = one entry in
`detect_accelerators()` + one EP preference row + optionally one profile.

**Benchmark flow**: every suite runs under a `ResourceMonitor`, so each
`BenchmarkResult` carries AI metrics (p50/p95/p99 latency, achievable Hz,
within-budget verdict against the control period) *and* hardware metrics
(CPU/GPU util, memory, power, temperature) *and* the full `SystemInfo` —
published simultaneously to:
- **W&B** (`WandbReporter`): SystemInfo becomes the run config; runs group
  by hardware profile so cross-hardware comparison is a built-in W&B view.
- **Prometheus/Grafana** (`PrometheusReporter`): `/metrics` endpoint to add
  as a scrape target, or node_exporter textfile on the Pi.
- **JSON** artifacts named `<timestamp>_<suite>_<machine-label>.json`.

The `PostTrainingLoop` accepts the same reporters, so continual-learning
iterations (success rate, loss, dataset growth) stream to W&B/Grafana
automatically.

Known telemetry gaps: Apple GPU utilization needs privileged
`powermetrics` (CPU/RAM only for now); Hailo/Coral report through their own
runtimes, not NVML.

## 11. Future scaling roadmap

Near term (current hardware):
- LeRobot export (`ReplayDataset.export_lerobot`) wired through
  `packages/robot_episode_dataset` → closes the SmolVLA fine-tune loop.
- Train `LearnedRewardModel` from accumulated VLM labels (script in
  `rewards/`); enable `task_likelihood` check at inference.
- GazeboEnv implementation over `ros2/topics.py` for hardware-in-the-loop.
- Surface IterationReports in the existing Grafana/W&B observability stack.

Mid term:
- Upgrade offline RL (IQL/CQL) behind the same `PolicyTrainer` interface;
  PER-driven TD-error priorities.
- Goal relabeling (`DataSource.AUGMENTED`) — hindsight experience replay on
  the episodic store.
- AI-judge ensembles (multiple `VLMClient`s, majority vote) and judge
  calibration against human labels.
- Curriculum + domain-randomization configs shared with
  `data_engine/isaac_sim/randomization_config.yaml`.

Long term:
- World-model adapter (interface slot exists: a world model is a
  `SimulationEnv` learned from the ReplayDataset — plugs into the same
  collectors and trainers).
- GR00T / RT-2-style adapters in `policies/adapters.py` (one class each).
- Fleet scale: ReplayDataset → object store (S3/MinIO) backend behind the
  same API; distributed collection from multiple robots; nightly continual
  runs via the scheduler.
- On-robot fast adaptation: LoRA-style incremental fine-tuning in
  `FineTuneTrainer` with `PolicyVersionManager` gating deployment on eval
  success-rate regression.
