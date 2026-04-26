#!/usr/bin/env python3
"""
Fine-tune SmolVLA on collected mobile manipulation episodes.

Usage:
  python train.py --dataset-path ~/datasets/mobile_manipulation \\
                  --output-dir ~/checkpoints/smolvla_mobile \\
                  --num-epochs 100
"""

import argparse
from pathlib import Path

try:
    import torch
    import torch.nn as nn
    from torch.utils.data import DataLoader
    from torch.optim import AdamW
    from torch.optim.lr_scheduler import CosineAnnealingLR

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    from lerobot.common.datasets.lerobot_dataset import LeRobotDataset
    from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False
    # Fallback: use the lightweight loader from the data_engine package
    try:
        from data_engine.loader.dataset import LeRobotDatasetLite as LeRobotDataset
    except ImportError:
        LeRobotDataset = None


# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Fine-tune SmolVLA on mobile manipulation dataset."
    )
    parser.add_argument(
        "--dataset-path",
        type=str,
        required=True,
        help="Path to the LeRobot dataset directory.",
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default="~/checkpoints/smolvla_mobile",
        help="Directory to save checkpoints.",
    )
    parser.add_argument(
        "--pretrained-model",
        type=str,
        default="lerobot/smolvla_base",
        help="HuggingFace model ID or local path for the base SmolVLA model.",
    )
    parser.add_argument(
        "--num-epochs", type=int, default=100, help="Number of training epochs."
    )
    parser.add_argument(
        "--batch-size", type=int, default=8, help="Training batch size."
    )
    parser.add_argument("--lr", type=float, default=1e-4, help="Initial learning rate.")
    parser.add_argument(
        "--device", type=str, default="cuda", help="Training device: cuda or cpu."
    )
    parser.add_argument(
        "--chunk-size", type=int, default=50, help="Action chunk size for SmolVLA."
    )
    parser.add_argument(
        "--save-every", type=int, default=10, help="Save checkpoint every N epochs."
    )
    parser.add_argument(
        "--grad-clip-norm", type=float, default=10.0, help="Gradient clipping norm."
    )
    parser.add_argument(
        "--num-workers", type=int, default=4, help="DataLoader worker processes."
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Training utilities
# ---------------------------------------------------------------------------


def get_device(device_str: str):
    if not TORCH_AVAILABLE:
        raise RuntimeError("PyTorch is required for training.")
    if device_str == "cuda" and torch.cuda.is_available():
        return torch.device("cuda")
    if device_str == "cuda":
        print("[WARNING] CUDA not available, falling back to CPU.")
    return torch.device("cpu")


def save_checkpoint(
    policy, optimizer, scheduler, epoch: int, loss: float, output_dir: Path
):
    ckpt_dir = output_dir / f"checkpoint_epoch_{epoch:04d}"
    ckpt_dir.mkdir(parents=True, exist_ok=True)

    if hasattr(policy, "save_pretrained"):
        policy.save_pretrained(str(ckpt_dir))
    else:
        torch.save(policy.state_dict(), ckpt_dir / "policy.pt")

    torch.save(
        {
            "epoch": epoch,
            "loss": loss,
            "optimizer_state": optimizer.state_dict(),
            "scheduler_state": scheduler.state_dict() if scheduler else None,
        },
        ckpt_dir / "train_state.pt",
    )

    print(f"  Checkpoint saved to {ckpt_dir}")


def compute_eval_loss(policy, dataloader, device) -> float:
    """Run one pass over the dataloader and return mean loss."""
    policy.eval()
    total_loss = 0.0
    num_batches = 0

    with torch.no_grad():
        for batch in dataloader:
            batch = {
                k: v.to(device) if hasattr(v, "to") else v for k, v in batch.items()
            }
            loss_dict = policy.forward(batch)
            loss = loss_dict["loss"] if isinstance(loss_dict, dict) else loss_dict
            total_loss += loss.item()
            num_batches += 1

    policy.train()
    return total_loss / max(num_batches, 1)


# ---------------------------------------------------------------------------
# Main training loop
# ---------------------------------------------------------------------------


def main():
    args = parse_args()

    if not TORCH_AVAILABLE:
        print("[ERROR] PyTorch is required. Install: pip install torch torchvision")
        return

    if not LEROBOT_AVAILABLE:
        print("[ERROR] lerobot is required. Install:")
        print(
            '  pip install "lerobot[feetech] @ git+https://github.com/huggingface/lerobot.git"'
        )
        return

    output_dir = Path(args.output_dir).expanduser()
    output_dir.mkdir(parents=True, exist_ok=True)
    dataset_path = Path(args.dataset_path).expanduser()

    device = get_device(args.device)

    print("=" * 60)
    print("  SmolVLA Mobile Manipulation — Fine-tuning")
    print("=" * 60)
    print(f"  Dataset:     {dataset_path}")
    print(f"  Output:      {output_dir}")
    print(f"  Base model:  {args.pretrained_model}")
    print(f"  Epochs:      {args.num_epochs}")
    print(f"  Batch size:  {args.batch_size}")
    print(f"  LR:          {args.lr}")
    print(f"  Device:      {device}")
    print("=" * 60)

    # ------------------------------------------------------------------
    # Load dataset
    # ------------------------------------------------------------------
    print("\nLoading dataset...")
    if not LEROBOT_AVAILABLE and LeRobotDataset is None:
        print("[ERROR] Neither lerobot nor data_engine is installed.")
        return
    if LEROBOT_AVAILABLE:
        train_dataset = LeRobotDataset(str(dataset_path))
    else:
        train_dataset = LeRobotDataset(str(dataset_path), chunk_size=args.chunk_size)
    # Use 90/10 train/eval split if dataset supports it
    split_idx = int(len(train_dataset) * 0.9)
    indices_train = list(range(split_idx))
    indices_eval = list(range(split_idx, len(train_dataset)))

    from torch.utils.data import Subset

    train_subset = Subset(train_dataset, indices_train)
    eval_subset = Subset(train_dataset, indices_eval)

    train_loader = DataLoader(
        train_subset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=args.num_workers,
        pin_memory=(device.type == "cuda"),
        drop_last=True,
    )
    eval_loader = DataLoader(
        eval_subset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=args.num_workers,
        pin_memory=(device.type == "cuda"),
    )
    print(f"  Train samples: {len(train_subset)} | Eval samples: {len(eval_subset)}")
    print(f"  Train batches: {len(train_loader)} per epoch")

    # ------------------------------------------------------------------
    # Load policy
    # ------------------------------------------------------------------
    print(f'\nLoading SmolVLAPolicy from "{args.pretrained_model}"...')
    policy = SmolVLAPolicy.from_pretrained(args.pretrained_model)
    policy = policy.to(device)
    policy.train()
    num_params = sum(p.numel() for p in policy.parameters() if p.requires_grad)
    print(f"  Trainable parameters: {num_params:,}")

    # ------------------------------------------------------------------
    # Optimizer and scheduler
    # ------------------------------------------------------------------
    optimizer = AdamW(policy.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = CosineAnnealingLR(
        optimizer, T_max=args.num_epochs, eta_min=args.lr * 0.01
    )

    # ------------------------------------------------------------------
    # Training loop
    # ------------------------------------------------------------------
    print("\nStarting training...\n")
    best_eval_loss = float("inf")

    for epoch in range(1, args.num_epochs + 1):
        policy.train()
        epoch_loss = 0.0
        num_batches = 0

        for batch_idx, batch in enumerate(train_loader):
            # Move tensors to device
            batch = {
                k: v.to(device) if hasattr(v, "to") else v for k, v in batch.items()
            }

            optimizer.zero_grad()

            # Forward pass — SmolVLA returns a dict with 'loss' key
            output = policy.forward(batch)
            if isinstance(output, dict):
                loss = output["loss"]
            else:
                loss = output

            loss.backward()

            # Gradient clipping
            nn.utils.clip_grad_norm_(policy.parameters(), args.grad_clip_norm)

            optimizer.step()

            epoch_loss += loss.item()
            num_batches += 1

            if batch_idx % 50 == 0:
                print(
                    f"  Epoch {epoch:4d}/{args.num_epochs} | "
                    f"Batch {batch_idx:4d}/{len(train_loader)} | "
                    f"Loss: {loss.item():.6f}"
                )

        scheduler.step()

        avg_train_loss = epoch_loss / max(num_batches, 1)

        # Eval
        if len(eval_subset) > 0:
            eval_loss = compute_eval_loss(policy, eval_loader, device)
            is_best = eval_loss < best_eval_loss
            if is_best:
                best_eval_loss = eval_loss
        else:
            eval_loss = float("nan")
            is_best = False

        lr_now = optimizer.param_groups[0]["lr"]
        print(
            f"Epoch {epoch:4d}/{args.num_epochs} | "
            f"Train loss: {avg_train_loss:.6f} | "
            f"Eval loss: {eval_loss:.6f} | "
            f"LR: {lr_now:.2e}" + (" [BEST]" if is_best else "")
        )

        # Save checkpoint
        if epoch % args.save_every == 0 or epoch == args.num_epochs:
            save_checkpoint(
                policy, optimizer, scheduler, epoch, avg_train_loss, output_dir
            )

        # Save best model separately
        if is_best:
            best_dir = output_dir / "best"
            best_dir.mkdir(parents=True, exist_ok=True)
            if hasattr(policy, "save_pretrained"):
                policy.save_pretrained(str(best_dir))
            print(f"  New best eval loss: {best_eval_loss:.6f} → saved to {best_dir}")

    # ------------------------------------------------------------------
    # Save final model
    # ------------------------------------------------------------------
    print("\nSaving final model...")
    final_dir = output_dir / "final"
    final_dir.mkdir(parents=True, exist_ok=True)
    if hasattr(policy, "save_pretrained"):
        policy.save_pretrained(str(final_dir))
    else:
        torch.save(policy.state_dict(), final_dir / "policy.pt")

    print("\nTraining complete.")
    print(f"  Final model:  {final_dir}")
    print(f"  Best model:   {output_dir / 'best'}")
    print(f"  Best eval loss: {best_eval_loss:.6f}")


if __name__ == "__main__":
    main()
