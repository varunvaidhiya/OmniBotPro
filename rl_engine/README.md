# rl_engine

Isaac Lab reinforcement-learning training infrastructure for OmniBot's
sim-to-real RL policies, plus ONNX export for on-robot inference. Two policies
are trained here:

- **nav** — base navigation (27-D obs → 3-D action: velocity deltas Δvx, Δvy, Δω)
- **arm** — arm precision control (30-D obs → 6-D action: joint position deltas)

The exported `.onnx` files are loaded by the ROS nodes in
`robot_ws/src/omnibot_rl/` (`rl_nav_node`, `rl_arm_node`).

> Requires Isaac Sim + Isaac Lab on a GPU PC. This is **not** part of the
> `colcon` workspace build — run it standalone.

## Layout

```
rl_engine/
├── scripts/
│   ├── train_nav.py        # PPO training for the base nav policy
│   └── train_arm.py        # PPO training for the arm policy
├── envs/
│   ├── omnibot_nav_env.py  # Mecanum base RL env
│   └── omnibot_arm_env.py  # SO-101 arm RL env
├── tasks/mdp/              # actions, observations, rewards, terminations
├── export/export_policy.py # checkpoint (.pt) → ONNX / TorchScript
├── config/                 # nav_train.yaml, arm_train.yaml,
│                           # domain_randomization.yaml, wandb_sweep_{nav,arm}.yaml
└── requirements.txt        # isaaclab>=1.1.0, torch>=2.1, onnxruntime-gpu>=1.16
```

## Setup

```bash
# Install Isaac Lab first (see requirements.txt header), then:
pip install -r rl_engine/requirements.txt
```

## Key design decisions

- Actions are velocity/position **deltas**, matching the Yahboom driver's
  0.05 m/s ramp limiter and the arm's ±0.05 rad/step clamp.
- `MecanumWheelActionTerm` uses the exact OmniBot constants
  (`lx=0.0825`, `ly=0.1075`, `r=0.04`).
- `ActionDelayTerm` (1–3 steps at 20 Hz = 50–150 ms) models servo bus latency.
- Domain randomization (`config/domain_randomization.yaml`) is disabled by
  default; enable it only during training.

## Train → Export workflow

```bash
# 1. Train (nav example)
python rl_engine/scripts/train_nav.py --num_envs 512 --max_iterations 2000 \
    --log_dir ~/logs/omnibot_nav

# 2. Export to ONNX
python rl_engine/export/export_policy.py \
    --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \
    --output ~/models/omnibot_nav_policy.onnx --type nav

# 3. Copy the .onnx to the robot and launch RL inference
#    ros2 launch omnibot_hybrid hybrid_robot.launch.py use_rl:=true
```

Arm policy: `train_arm.py` (defaults `--num_envs 256 --max_iterations 3000`)
then `export_policy.py --type arm`.

Common flags — train: `--num_envs`, `--max_iterations`, `--log_dir`, `--resume`,
`--seed`. Export: `--checkpoint`, `--output`, `--type {nav,arm}`, `--both`
(ONNX + TorchScript), `--wandb_project`, `--wandb_source_run`.

## W&B

`train_nav.py`/`train_arm.py` accept `--wandb_project`; Bayesian sweeps live in
`config/wandb_sweep_nav.yaml` and `config/wandb_sweep_arm.yaml`.

## See also

- `robot_ws/src/omnibot_rl/` — on-robot ONNX inference nodes + config
- `learning_engine/` — the `online_rl` trainer delegates to this engine
