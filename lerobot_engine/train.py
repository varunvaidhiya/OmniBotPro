#!/usr/bin/env python3
"""Fine-tune any registered visuomotor policy on OmniBot mobile manipulation data.

Usage
-----
# SmolVLA (default)
python lerobot_engine/train.py \\
    --model smolvla \\
    --checkpoint lerobot/smolvla_base \\
    --dataset-path ~/datasets/mobile_manipulation \\
    --output-dir ~/checkpoints/smolvla_run1 \\
    --wandb-project omnibot_smolvla \\
    --wandb-run-name "smolvla-lr1e-4-bs8"

# ACT baseline
python lerobot_engine/train.py \\
    --model act \\
    --checkpoint lerobot/act_base \\
    --dataset-path ~/datasets/mobile_manipulation \\
    --output-dir ~/checkpoints/act_run1

# Diffusion Policy
python lerobot_engine/train.py \\
    --model diffusion \\
    --checkpoint lerobot/diffusion_pusht \\
    --dataset-path ~/datasets/mobile_manipulation \\
    --output-dir ~/checkpoints/diffusion_run1

Available models
----------------
    python lerobot_engine/train.py --list-models
"""

import argparse
from pathlib import Path

try:
    import torch
    import torch.nn as nn
    from torch.utils.data import DataLoader, Subset
    from torch.optim import AdamW
    from torch.optim.lr_scheduler import CosineAnnealingLR

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    from lerobot.common.datasets.lerobot_dataset import LeRobotDataset

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False
    try:
        from data_engine.loader.dataset import LeRobotDatasetLite as LeRobotDataset
    except ImportError:
        LeRobotDataset = None

try:
    import wandb

    WANDB_AVAILABLE = True
except ImportError:
    WANDB_AVAILABLE = False

from lerobot_engine.models import list_models, make_policy


# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Fine-tune a visuomotor policy on OmniBot mobile manipulation data.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument(
        "--model",
        type=str,
        default="smolvla",
        help="Model type from the registry. Run --list-models to see all options.",
    )
    parser.add_argument(
        "--list-models",
        action="store_true",
        help="Print registered model names and exit.",
    )
    parser.add_argument(
        "--checkpoint",
        type=str,
        default=None,
        help="HuggingFace hub ID or local path for the pretrained checkpoint.",
    )
    parser.add_argument(
        "--dataset-path",
        type=str,
        required=False,
        default=None,
        help="Path to the LeRobot dataset directory.",
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default="~/checkpoints/policy_run",
        help="Directory to save checkpoints.",
    )
    parser.add_argument("--num-epochs", type=int, default=100)
    parser.add_argument("--batch-size", type=int, default=8)
    parser.add_argument("--lr", type=float, default=1e-4)
    parser.add_argument("--device", type=str, default="cuda")
    parser.add_argument("--chunk-size", type=int, default=50)
    parser.add_argument("--save-every", type=int, default=10)
    parser.add_argument("--grad-clip-norm", type=float, default=10.0)
    parser.add_argument("--num-workers", type=int, default=4)
    # W&B
    parser.add_argument(
        "--wandb-project",
        type=str,
        default="",
        help="W&B project name. Leave empty to disable W&B logging.",
    )
    parser.add_argument(
        "--wandb-run-name",
        type=str,
        default=None,
        help="W&B run name (auto-generated when omitted).",
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Defaults per model type
# ---------------------------------------------------------------------------

_MODEL_DEFAULTS = {
    "smolvla": {"checkpoint": "lerobot/smolvla_base", "lr": 1e-4, "batch_size": 8},
    "act": {"checkpoint": "lerobot/act_base", "lr": 1e-4, "batch_size": 16},
    "diffusion": {
        "checkpoint": "lerobot/diffusion_pusht",
        "lr": 1e-4,
        "batch_size": 64,
    },
    "openvla": {"checkpoint": "openvla/openvla-7b", "lr": 2e-5, "batch_size": 4},
}


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def get_device(device_str: str):
    if not TORCH_AVAILABLE:
        raise RuntimeError("PyTorch is required for training.")
    if device_str == "cuda" and torch.cuda.is_available():
        return torch.device("cuda")
    if device_str == "cuda":
        print("[WARNING] CUDA not available, falling back to CPU.")
    return torch.device("cpu")


def save_checkpoint(policy_model, optimizer, scheduler, epoch, loss, output_dir):
    ckpt_dir = output_dir / f"checkpoint_epoch_{epoch:04d}"
    ckpt_dir.mkdir(parents=True, exist_ok=True)

    if hasattr(policy_model, "save_pretrained"):
        policy_model.save_pretrained(str(ckpt_dir))
    else:
        torch.save(policy_model.state_dict(), ckpt_dir / "policy.pt")

    torch.save(
        {
            "epoch": epoch,
            "loss": loss,
            "optimizer_state": optimizer.state_dict(),
            "scheduler_state": scheduler.state_dict() if scheduler else None,
        },
        ckpt_dir / "train_state.pt",
    )
    print(f"  Checkpoint saved → {ckpt_dir}")
    return ckpt_dir


def compute_eval_loss(policy_model, dataloader, device) -> float:
    policy_model.eval()
    total, n = 0.0, 0
    with torch.no_grad():
        for batch in dataloader:
            batch = {
                k: v.to(device) if hasattr(v, "to") else v for k, v in batch.items()
            }
            out = policy_model.forward(batch)
            loss = out["loss"] if isinstance(out, dict) else out
            total += loss.item()
            n += 1
    policy_model.train()
    return total / max(n, 1)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main():
    args = parse_args()

    if args.list_models:
        print("Registered models:", list_models())
        return

    if not TORCH_AVAILABLE:
        print("[ERROR] PyTorch is required: pip install torch torchvision")
        return

    if not LEROBOT_AVAILABLE:
        print("[ERROR] lerobot is required:")
        print(
            '  pip install "lerobot @ git+https://github.com/huggingface/lerobot.git"'
        )
        return

    if args.dataset_path is None:
        print("[ERROR] --dataset-path is required.")
        return

    # Apply per-model defaults for unset args
    defaults = _MODEL_DEFAULTS.get(args.model, {})
    checkpoint = args.checkpoint or defaults.get("checkpoint")
    if checkpoint is None:
        print(f"[ERROR] --checkpoint is required for model '{args.model}'.")
        return

    output_dir = Path(args.output_dir).expanduser()
    output_dir.mkdir(parents=True, exist_ok=True)
    dataset_path = Path(args.dataset_path).expanduser()
    device = get_device(args.device)

    print("=" * 60)
    print("  OmniBot — Policy Training")
    print("=" * 60)
    print(f"  Model:       {args.model}")
    print(f"  Checkpoint:  {checkpoint}")
    print(f"  Dataset:     {dataset_path}")
    print(f"  Output:      {output_dir}")
    print(f"  Epochs:      {args.num_epochs}")
    print(f"  Batch size:  {args.batch_size}")
    print(f"  LR:          {args.lr}")
    print(f"  Device:      {device}")
    print("=" * 60)

    # ── W&B initialisation ─────────────────────────────────────────────────
    wandb_run = None
    if args.wandb_project and WANDB_AVAILABLE:
        wandb_run = wandb.init(
            project=args.wandb_project,
            name=args.wandb_run_name,
            config={
                "model": args.model,
                "checkpoint": checkpoint,
                "num_epochs": args.num_epochs,
                "batch_size": args.batch_size,
                "lr": args.lr,
                "chunk_size": args.chunk_size,
                "grad_clip_norm": args.grad_clip_norm,
                "device": str(device),
            },
        )
        print(f"  W&B run: {wandb_run.url}")
    elif args.wandb_project and not WANDB_AVAILABLE:
        print("[WARNING] --wandb-project set but wandb is not installed. Skipping.")

    # Load dataset
    print("\nLoading dataset...")
    if LeRobotDataset is None:
        print("[ERROR] Neither lerobot nor data_engine is installed.")
        return
    dataset = LeRobotDataset(str(dataset_path))
    split = int(len(dataset) * 0.9)
    train_loader = DataLoader(
        Subset(dataset, list(range(split))),
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=args.num_workers,
        pin_memory=(device.type == "cuda"),
        drop_last=True,
    )
    eval_loader = DataLoader(
        Subset(dataset, list(range(split, len(dataset)))),
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=args.num_workers,
        pin_memory=(device.type == "cuda"),
    )
    print(
        f"  Train: {split} | Eval: {len(dataset) - split} | Batches/epoch: {len(train_loader)}"
    )

    # Load policy via registry
    print(f'\nLoading {args.model} from "{checkpoint}"...')
    adapter = make_policy(args.model, checkpoint=checkpoint, device=str(device))
    policy_model = adapter.model
    if policy_model is None:
        print("[ERROR] This adapter does not expose a trainable model.")
        return

    policy_model.train()
    n_params = sum(p.numel() for p in policy_model.parameters() if p.requires_grad)
    print(f"  Trainable params: {n_params:,}")

    optimizer = AdamW(policy_model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = CosineAnnealingLR(
        optimizer, T_max=args.num_epochs, eta_min=args.lr * 0.01
    )

    print("\nStarting training...\n")
    best_eval_loss = float("inf")
    global_step = 0
    epoch = 0

    try:
        for epoch in range(1, args.num_epochs + 1):
            policy_model.train()
        epoch_loss, n_batches = 0.0, 0

        for batch_idx, batch in enumerate(train_loader):
            batch = {
                k: v.to(device) if hasattr(v, "to") else v for k, v in batch.items()
            }
            optimizer.zero_grad()
            out = policy_model.forward(batch)
            loss = out["loss"] if isinstance(out, dict) else out
            loss.backward()
            grad_norm = nn.utils.clip_grad_norm_(
                policy_model.parameters(), args.grad_clip_norm
            )
            optimizer.step()
            epoch_loss += loss.item()
            n_batches += 1
            global_step += 1

            if batch_idx % 50 == 0:
                print(
                    f"  Epoch {epoch:4d}/{args.num_epochs} | "
                    f"Batch {batch_idx:4d}/{len(train_loader)} | "
                    f"Loss: {loss.item():.6f}"
                )
                if wandb_run is not None:
                    wandb.log(
                        {
                            "train/step_loss": loss.item(),
                            "train/grad_norm": float(grad_norm),
                            "train/step": global_step,
                        },
                        step=global_step,
                    )

        scheduler.step()
        avg_loss = epoch_loss / max(n_batches, 1)
        eval_loss = (
            compute_eval_loss(policy_model, eval_loader, device)
            if eval_loader and len(eval_loader.dataset) > 0
            else float("nan")
        )
        is_best = eval_loss < best_eval_loss

        if is_best:
            best_eval_loss = eval_loss

        print(
            f"Epoch {epoch:4d}/{args.num_epochs} | "
            f"Train: {avg_loss:.6f} | Eval: {eval_loss:.6f} | "
            f"LR: {optimizer.param_groups[0]['lr']:.2e}"
            + (" [BEST]" if is_best else "")
        )

        if wandb_run is not None:
            wandb.log(
                {
                    "train/epoch_loss": avg_loss,
                    "eval/loss": eval_loss,
                    "train/lr": optimizer.param_groups[0]["lr"],
                    "eval/is_best": int(is_best),
                    "epoch": epoch,
                },
                step=global_step,
            )

        if epoch % args.save_every == 0 or epoch == args.num_epochs:
            ckpt_dir = save_checkpoint(
                policy_model, optimizer, scheduler, epoch, avg_loss, output_dir
            )
            if wandb_run is not None:
                artifact = wandb.Artifact(
                    f"omnibot-{args.model}-ckpt",
                    type="model",
                    metadata={"epoch": epoch, "eval_loss": eval_loss},
                )
                artifact.add_dir(str(ckpt_dir))
                wandb_run.log_artifact(artifact, aliases=[f"epoch-{epoch:04d}"])

        if is_best:
            best_dir = output_dir / "best"
            best_dir.mkdir(parents=True, exist_ok=True)
            if hasattr(policy_model, "save_pretrained"):
                policy_model.save_pretrained(str(best_dir))
            if wandb_run is not None:
                best_artifact = wandb.Artifact(
                    f"omnibot-{args.model}-best",
                    type="model",
                    metadata={"epoch": epoch, "eval_loss": eval_loss},
                )
                best_artifact.add_dir(str(best_dir))
                wandb_run.log_artifact(
                    best_artifact, aliases=["best", f"epoch-{epoch:04d}"]
                )

    except Exception:
        print(f"\nTraining interrupted at epoch {epoch}")
        raise
    finally:
        if wandb_run is not None:
            wandb.summary["best_eval_loss"] = best_eval_loss
            wandb_run.finish()

    print(f"\nTraining complete. Best eval loss: {best_eval_loss:.6f}")
    print(f"  Best checkpoint: {output_dir / 'best'}")


if __name__ == "__main__":
    main()
