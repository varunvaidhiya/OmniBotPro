#!/usr/bin/env python3
"""
Export Isaac Lab-trained policies to ONNX and TorchScript for ROS 2 deployment.

Usage
─────
  # Export navigation policy
  python rl_engine/export/export_policy.py \\
    --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \\
    --output ~/models/omnibot_nav_policy.onnx \\
    --type nav

  # Export arm policy
  python rl_engine/export/export_policy.py \\
    --checkpoint ~/logs/omnibot_arm/checkpoints/model_3000.pt \\
    --output ~/models/omnibot_arm_policy.onnx \\
    --type arm

  # Export both formats (ONNX + TorchScript fallback)
  python rl_engine/export/export_policy.py \\
    --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \\
    --output ~/models/omnibot_nav_policy \\
    --type nav --both

The exported ONNX models are loaded by rl_nav_node.py and rl_arm_node.py.
Observation dimensions MUST match the training environment exactly.
"""

import argparse
import os

import torch

# Observation/action dimensions matching the training environments
OBS_DIMS = {
    "nav": 27,  # from omnibot_nav_env.py
    "arm": 30,  # from omnibot_arm_env.py
}
ACTION_DIMS = {
    "nav": 3,
    "arm": 6,
}


def load_policy(checkpoint_path: str, obs_dim: int, action_dim: int) -> torch.nn.Module:
    """
    Load a trained RSL-RL actor network from a checkpoint.

    RSL-RL saves the full runner state. The actor is nested under
    'model_state_dict' → actor network state dict.
    """
    ckpt = torch.load(checkpoint_path, map_location="cpu")

    # RSL-RL checkpoint structure
    if "model_state_dict" in ckpt:
        state_dict = ckpt["model_state_dict"]
    elif "ac_state_dict" in ckpt:
        state_dict = ckpt["ac_state_dict"]
    else:
        state_dict = ckpt

    # Build a simple MLP actor matching the network config (256, 128, 64 + ELU)
    actor = _build_mlp_actor(obs_dim, action_dim)

    # Filter state dict to actor keys only
    actor_state = {
        k.replace("actor.", ""): v
        for k, v in state_dict.items()
        if k.startswith("actor.")
    }
    if not actor_state:
        raise RuntimeError(
            "No 'actor.*' keys found in checkpoint. "
            "Ensure the checkpoint was saved by RSL-RL with the actor-critic architecture. "
            f"Available top-level keys: {list(state_dict.keys())[:10]}"
        )

    actor.load_state_dict(actor_state, strict=True)
    return actor


def _build_mlp_actor(
    obs_dim: int, action_dim: int, hidden: tuple = (256, 128, 64)
) -> torch.nn.Module:
    """Build MLP actor matching the network architecture in nav_train.yaml."""
    layers = []
    in_dim = obs_dim
    for h in hidden:
        layers += [torch.nn.Linear(in_dim, h), torch.nn.ELU()]
        in_dim = h
    layers.append(torch.nn.Linear(in_dim, action_dim))
    return torch.nn.Sequential(*layers)


def export_onnx(model: torch.nn.Module, obs_dim: int, output_path: str) -> None:
    """Export model to ONNX opset 17."""
    model.eval()
    dummy_obs = torch.zeros(1, obs_dim)
    torch.onnx.export(
        model,
        dummy_obs,
        output_path,
        input_names=["obs"],
        output_names=["action_mean"],
        dynamic_axes={
            "obs": {0: "batch"},
            "action_mean": {0: "batch"},
        },
        opset_version=17,
        do_constant_folding=True,
        verbose=False,
    )
    print(f"Exported ONNX policy: {output_path}")
    _verify_onnx(output_path, obs_dim)


def _verify_onnx(onnx_path: str, obs_dim: int) -> None:
    """Quick verification that the exported ONNX model runs correctly."""
    try:
        import onnxruntime as ort
        import numpy as np

        sess = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"])
        dummy = np.zeros((1, obs_dim), dtype=np.float32)
        out = sess.run(None, {"obs": dummy})
        print(f"  ONNX verification: output shape {out[0].shape} — OK")
    except ImportError:
        print("  (onnxruntime not installed, skipping verification)")
    except Exception as exc:
        print(f"  WARNING: ONNX verification failed: {exc}")


def export_torchscript(model: torch.nn.Module, obs_dim: int, output_path: str) -> None:
    """Export model to TorchScript (fallback if ONNX Runtime not available)."""
    model.eval()
    dummy_obs = torch.zeros(1, obs_dim)
    try:
        scripted = torch.jit.trace(model, dummy_obs)
        scripted.save(output_path)
        print(f"Exported TorchScript policy: {output_path}")
    except Exception as exc:
        print(f"TorchScript export failed: {exc}")
        try:
            scripted = torch.jit.script(model)
            scripted.save(output_path)
            print(f"Exported TorchScript (scripted): {output_path}")
        except Exception as exc2:
            print(f"TorchScript scripted export also failed: {exc2}")


def main():
    parser = argparse.ArgumentParser(description="Export OmniBot RL policy to ONNX")
    parser.add_argument(
        "--checkpoint",
        required=True,
        help="Path to Isaac Lab RSL-RL checkpoint (.pt file)",
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Output path (without extension for --both mode)",
    )
    parser.add_argument(
        "--type",
        choices=["nav", "arm"],
        required=True,
        help="Policy type: nav (27D obs, 3D act) or arm (30D obs, 6D act)",
    )
    parser.add_argument(
        "--both", action="store_true", help="Export both ONNX and TorchScript formats"
    )
    parser.add_argument(
        "--wandb_project",
        type=str,
        default="",
        help="W&B project name for artifact tracking. Leave empty to skip.",
    )
    parser.add_argument(
        "--wandb_source_run",
        type=str,
        default="",
        help="W&B run ID of the training run that produced this checkpoint.",
    )
    args = parser.parse_args()

    checkpoint = os.path.expanduser(args.checkpoint)
    output = os.path.expanduser(args.output)
    obs_dim = OBS_DIMS[args.type]
    action_dim = ACTION_DIMS[args.type]

    if not os.path.exists(checkpoint):
        print(f"ERROR: Checkpoint not found: {checkpoint}")
        return

    print(f"Loading {args.type} policy from {checkpoint}")
    print(f"  obs_dim={obs_dim}, action_dim={action_dim}")

    model = load_policy(checkpoint, obs_dim, action_dim)
    model.eval()

    onnx_path = None
    if args.both:
        onnx_path = output + ".onnx"
        export_onnx(model, obs_dim, onnx_path)
        export_torchscript(model, obs_dim, output + ".pt")
    elif output.endswith(".pt"):
        export_torchscript(model, obs_dim, output)
    else:
        onnx_path = output if output.endswith(".onnx") else output + ".onnx"
        export_onnx(model, obs_dim, onnx_path)

    if args.wandb_project and onnx_path and os.path.exists(onnx_path):
        try:
            import wandb

            tags = ["export", args.type]
            init_kwargs = dict(project=args.wandb_project, job_type="export", tags=tags)
            if args.wandb_source_run:
                init_kwargs["id"] = None  # new run, but linked via artifact lineage
            run = wandb.init(**init_kwargs)
            artifact = wandb.Artifact(
                f"omnibot-{args.type}-onnx",
                type="model",
                metadata={
                    "checkpoint": checkpoint,
                    "obs_dim": obs_dim,
                    "action_dim": action_dim,
                    "opset": 17,
                },
            )
            artifact.add_file(onnx_path)
            aliases = ["latest", f"{args.type}-onnx"]
            if args.wandb_source_run:
                aliases.append(f"from-{args.wandb_source_run[:8]}")
            run.log_artifact(artifact, aliases=aliases)
            run.finish()
            print(f"  W&B artifact logged to project '{args.wandb_project}'")
        except ImportError:
            print("  (wandb not installed, skipping artifact tracking)")


if __name__ == "__main__":
    main()
