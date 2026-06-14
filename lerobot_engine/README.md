# lerobot_engine

Direct [LeRobot](https://github.com/huggingface/lerobot)-based training,
recording, and inference for OmniBot's unified 9-DOF mobile-manipulation policy
(6 arm joints + 3 base velocities). **No ROS required** — these are standalone
scripts that run on the GPU workstation. The deployed inference path is the ROS
`policy_node` in `robot_ws/src/omnibot_lerobot/`; this engine produces the
checkpoints it loads.

## Layout

```
lerobot_engine/
├── train.py                 # Fine-tune any registered policy on a LeRobot dataset
├── record.py                # Standalone gamepad + webcam dataset recorder (no ROS)
├── infer.py                 # Standalone inference test against a live/dummy camera
├── models/                  # Model registry + adapters
│   ├── registry.py          # register() / make_policy() / list_models()
│   ├── base.py              # PolicyAdapter interface
│   ├── smolvla.py  act.py  diffusion.py  openvla.py
├── configs/
│   ├── models/              # smolvla.yaml, act.yaml, diffusion.yaml
│   └── robot/               # omnibot_mobile_manip.yaml (9-DOF state/action, cameras)
├── requirements.txt
└── wandb_sweep_smolvla.yaml # Bayesian sweep for SmolVLA fine-tuning
```

## Setup

```bash
pip install -r lerobot_engine/requirements.txt
```

## Models

The registry (`models/registry.py`) selects the backend by name. Available
adapters: `smolvla` (default), `act`, `diffusion`, `openvla`.

```bash
python lerobot_engine/train.py --list-models
```

These names match `policy_node`'s `model_type` parameter, so a model trained
here drops directly into `ros2 launch omnibot_lerobot policy_inference.launch.py
model_type:=<name> checkpoint:=<path>`.

## Train

```bash
python lerobot_engine/train.py \
    --model smolvla \
    --dataset-path /data/lerobot/pick_place \
    --output-dir ~/checkpoints/smolvla_run1 \
    --num-epochs 100 --batch-size 8 --lr 1e-4 --chunk-size 50 \
    --wandb-project omnibot_smolvla
```

Key flags: `--model`, `--checkpoint` (pretrained hub id or path), `--dataset-path`,
`--output-dir`, `--num-epochs`, `--batch-size`, `--lr`, `--device`,
`--chunk-size`, `--save-every`, `--grad-clip-norm`, `--num-workers`,
`--wandb-project`, `--wandb-run-name`.

## Record (standalone, no ROS)

```bash
python lerobot_engine/record.py     # RB = start/stop, LB = discard
```

For the full ROS-integrated recording workflow (leader arm + base + ROS topics),
use `ros2 launch omnibot_lerobot teleop_record.launch.py` instead.

## Inference test (standalone, no ROS)

```bash
python lerobot_engine/infer.py \
    --model smolvla \
    --checkpoint ~/checkpoints/smolvla_run1/best \
    --task "pick up the red cube"
```

Overlays the predicted arm + base actions on the camera feed (use a dummy frame
when no camera is attached).

## See also

- `data_engine/TRAINING_GUIDE.md` — end-to-end recording → dataset → training loop
- `data_engine/` — ROS bag → LeRobot dataset conversion
- `robot_ws/src/omnibot_lerobot/` — the ROS deployment node (`policy_node`)
