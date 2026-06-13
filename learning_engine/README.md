# OmniBot Learning Engine

Modular post-training & continual-learning framework for OmniBot. The robot
improves from demonstrations, simulation, real-world execution,
self-evaluation and reward feedback. Full design: [ARCHITECTURE.md](ARCHITECTURE.md).

## Install

The core needs only numpy. Everything heavier is an optional extra you add
when you need it, so the same package installs on the Pi, in CI, and on a GPU
workstation.

```bash
# Core (numpy only) — runs the full test suite
pip install -e learning_engine

# Optional extras (mix as needed):
#   train → torch (trainers, learned reward model)   onnx  → run exported RL policies
#   vlm   → anthropic + pillow (Claude judges)        sim   → gymnasium (MuJoCo/ManiSkill)
#   bench → psutil + pynvml + wandb (benchmarking)    yaml  → load YAML configs
pip install -e "learning_engine[train,vlm,onnx,bench]"   # typical GPU workstation
```

## Run the tests

```bash
python3 -m unittest discover -s learning_engine/tests -t .
```

## Quickstart: one post-training iteration in simulation

```python
from learning_engine.data.collectors import SimRolloutCollector
from learning_engine.data.replay_dataset import ReplayDataset
from learning_engine.evaluation.self_eval import HeuristicSelfEvaluator
from learning_engine.loop import LoopComponents, PostTrainingLoop
from learning_engine.policies.base import RandomPolicy
from learning_engine.policies.trainers import BehaviorCloningTrainer
from learning_engine.replay.buffer import EpisodicReplayStore
from learning_engine.rewards.engine import RewardEngine
from learning_engine.core.registry import ENVS

env = ENVS.create("mujoco", env_id="Pusher-v5")        # or isaac_lab / maniskill
policy = RandomPolicy(action_dim=env.action_dim)
dataset = ReplayDataset("~/datasets/omnibot_replay")

loop = PostTrainingLoop(LoopComponents(
    collectors=[SimRolloutCollector(env, policy, task_instruction="push the puck")],
    dataset=dataset,
    reward_engine=RewardEngine.from_config([
        {"name": "task_success"}, {"name": "time"},
        {"name": "action_smoothness", "weight": 0.5},
    ]),
    trainer=BehaviorCloningTrainer(device="auto"),   # cuda/mps/cpu, auto-detected
    policy=policy,
    evaluators=[HeuristicSelfEvaluator()],
    replay_store=EpisodicReplayStore(dataset),
))
report = loop.run_iteration()
print(report.to_dict())
```

## Quickstart: record real-robot episodes (on the Pi)

```bash
python3 -m learning_engine.ros2.episode_logger_node --ros-args \
    -p output_dir:=~/datasets/execution_logs

# Manual segmentation (or rely on /mission/status auto-segmentation):
ros2 topic pub --once /learning/episode/start std_msgs/msg/String "data: 'pick up the red cup'"
ros2 topic pub --once /learning/episode/stop  std_msgs/msg/String "data: 'success'"
```

Then on the workstation, `ExecutionLogCollector("~/datasets/execution_logs")`
ingests the episodes into the learning loop.

## Quickstart: continual learning

```python
from learning_engine.loop import ContinualLearningScheduler

scheduler = ContinualLearningScheduler(
    loop, dataset, min_new_episodes=50,
    state_path="~/datasets/omnibot_replay/continual_state.json",
)
scheduler.step()          # trains only when triggers fire
print(scheduler.pending_triggers())
```

## Quickstart: verified inference (Best-of-N + safety)

```python
from learning_engine.verification import InferenceVerifier, SafetyCheck, ReachabilityCheck

verifier = InferenceVerifier(checks=[SafetyCheck(), ReachabilityCheck()])
result = verifier.select(policy, observation, task="pick up the red cup")
action = result.action     # safe, or the zero-action fallback
```

## Run on different target hardware

Nothing in the code names a machine — components take `device="auto"` and
ONNX policies pick their own execution providers, both resolved by
`learning_engine.hardware`. The same code runs on the Pi, a Jetson, the GPU
workstation, or Apple M-series.

```python
from learning_engine.hardware import describe, detect_profile, resolve_device

print(describe())               # accelerators, torch device, ONNX providers
print(detect_profile().name)    # pi_workstation | jetson_single | mac_dev | ...
resolve_device("auto")          # -> "cuda" / "mps" / "cpu" for this machine
```

Force a topology with `OMNIBOT_HW_PROFILE=jetson_single` (env var wins
everywhere). Built-in profiles: `pi_workstation`,
`pi_accelerator_workstation` (Hailo/Coral), `jetson_single`,
`workstation_single`, `mac_dev`.

## Benchmark any target & auto-publish

```bash
# Plumbing check anywhere (random policy, JSON results only)
python3 -m learning_engine.benchmarks.run --suite inference,training,dataset

# ONNX RL policy on Jetson — verify it fits the 20 Hz (50 ms) control budget,
# publish to W&B and expose /metrics for the existing Prometheus
python3 -m learning_engine.benchmarks.run --suite inference \
    --policy onnx --policy-kwargs '{"model_path": "~/models/omnibot_nav_policy.onnx"}' \
    --budget-ms 50 --wandb-project omnibot_benchmarks --prom-port 8890 --hold

# SmolVLA on the workstation / Mac (device auto-resolves to cuda / mps)
python3 -m learning_engine.benchmarks.run --suite inference \
    --policy smolvla --budget-ms 100 --wandb-project omnibot_benchmarks
```

Every result bundles AI metrics (p50/p95/p99 latency, achievable Hz,
within-budget verdict) + hardware telemetry (CPU/GPU util, memory, power,
temp via the `ResourceMonitor`) + a full `SystemInfo` snapshot, so one W&B
project compares Pi vs Jetson vs workstation vs Mac directly. On the Pi,
write a node_exporter textfile instead of serving a port:
`--prom-textfile ~/textfile_collector/omnibot_bench.prom`.

The `PostTrainingLoop` takes the same reporters (`reporters=[...]`), so
continual-learning iterations stream to W&B/Grafana automatically.

## Layout

See `configs/learning_loop.yaml` for a fully-commented example configuration,
and ARCHITECTURE.md §2 for the folder map. Everything is assembled from
registries (`learning_engine.core.registry`) — new collectors, reward terms,
trainers, policies, evaluators and checks plug in with a one-line decorator.
