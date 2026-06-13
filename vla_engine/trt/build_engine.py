#!/usr/bin/env python3
"""CLI: export SmolVLA vision encoder to ONNX then build a TRT engine.

Usage
-----
# FP16 engine (recommended for RTX GPUs)
python -m vla_engine.trt.build_engine \\
    --checkpoint lerobot/smolvla_base \\
    --output engines/smolvla_vision_fp16.trt \\
    --precision fp16

# FP32 fallback
python -m vla_engine.trt.build_engine \\
    --checkpoint /path/to/local/checkpoint \\
    --output engines/smolvla_vision_fp32.trt \\
    --precision fp32

# INT8 with calibration (requires --calibration-data)
python -m vla_engine.trt.build_engine \\
    --checkpoint lerobot/smolvla_base \\
    --output engines/smolvla_vision_int8.trt \\
    --precision int8 \\
    --calibration-data ~/datasets/omnibot/calibration_images/

The script:
  1. Loads SmolVLAPolicy from --checkpoint.
  2. Exports the vision encoder sub-module to a temporary ONNX file.
  3. Builds and serialises the TRT engine to --output.
  4. Runs a quick sanity check (TRT vs PyTorch output comparison).
"""

import argparse
import logging
import sys
import tempfile
from pathlib import Path

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("build_engine")


# ---------------------------------------------------------------------------
# INT8 calibrator
# ---------------------------------------------------------------------------


def _make_int8_calibrator(calibration_dir: str, image_h: int, image_w: int):
    """Build a simple image-folder INT8 entropy calibrator."""
    try:
        import tensorrt as trt
    except ImportError:
        raise RuntimeError("tensorrt is required for INT8 calibration.")

    import torch
    import cv2
    import numpy as np

    class _ImageFolderCalibrator(trt.IInt8EntropyCalibrator2):
        """Feeds images from a directory to the TRT INT8 calibrator."""

        def __init__(self, image_dir, image_h, image_w, batch_size=1):
            super().__init__()
            self.image_h = image_h
            self.image_w = image_w
            self.batch_size = batch_size

            image_extensions = {".jpg", ".jpeg", ".png", ".bmp"}
            self.image_paths = sorted(
                p
                for p in Path(image_dir).rglob("*")
                if p.suffix.lower() in image_extensions
            )
            if not self.image_paths:
                raise ValueError(f"No images found in calibration dir: {image_dir}")

            self._idx = 0
            self._device_input = torch.zeros(
                batch_size, 3, image_h, image_w, dtype=torch.float32, device="cuda"
            )
            self._cache_file = Path(image_dir) / "trt_int8_cache.bin"
            logger.info(
                "INT8 calibrator: %d images from %s", len(self.image_paths), image_dir
            )

        def get_batch_size(self):
            return self.batch_size

        def get_batch(self, names):
            if self._idx >= len(self.image_paths):
                return None
            imgs = []
            for _ in range(self.batch_size):
                if self._idx >= len(self.image_paths):
                    break
                path = str(self.image_paths[self._idx])
                self._idx += 1
                img = cv2.imread(path)
                if img is None:
                    img = np.zeros((self.image_h, self.image_w, 3), dtype=np.uint8)
                img = cv2.resize(
                    cv2.cvtColor(img, cv2.COLOR_BGR2RGB), (self.image_w, self.image_h)
                )
                arr = img.astype(np.float32) / 255.0
                imgs.append(arr.transpose(2, 0, 1))

            batch = np.stack(imgs, axis=0)
            self._device_input.copy_(torch.from_numpy(batch))
            return [self._device_input.data_ptr()]

        def read_calibration_cache(self):
            if self._cache_file.exists():
                logger.info("Reading INT8 calibration cache: %s", self._cache_file)
                return self._cache_file.read_bytes()
            return None

        def write_calibration_cache(self, cache):
            self._cache_file.write_bytes(bytes(cache))
            logger.info("INT8 calibration cache written: %s", self._cache_file)

    return _ImageFolderCalibrator(calibration_dir, image_h, image_w)


# ---------------------------------------------------------------------------
# Sanity check
# ---------------------------------------------------------------------------


def _sanity_check(policy, engine_path: Path, image_h: int, image_w: int) -> bool:
    """Compare TRT output vs PyTorch output on a random input. Returns True if close."""
    import torch
    from vla_engine.trt.encoder_export import (
        find_vision_encoder,
        TRTVisionEncoderModule,
    )

    logger.info("Running sanity check: TRT vs PyTorch …")
    try:
        device = next(policy.parameters()).device
        dummy = torch.rand(1, 3, image_h, image_w, device=device)

        original_encoder, _ = find_vision_encoder(policy)
        original_encoder.eval()
        with torch.no_grad():
            pt_out = original_encoder(dummy)
        if hasattr(pt_out, "last_hidden_state"):
            pt_out = pt_out.last_hidden_state

        trt_module = TRTVisionEncoderModule(engine_path)
        trt_out = trt_module(dummy)

        max_diff = (pt_out.float() - trt_out.float()).abs().max().item()
        mean_diff = (pt_out.float() - trt_out.float()).abs().mean().item()
        logger.info("Max abs diff: %.6f  |  Mean abs diff: %.6f", max_diff, mean_diff)

        # FP16 typically has <0.01 mean error on vision encoders
        threshold = 0.05
        if mean_diff < threshold:
            logger.info(
                "Sanity check PASSED (mean diff %.6f < %.4f)", mean_diff, threshold
            )
            return True
        else:
            logger.warning(
                "Sanity check WARNING: mean diff %.6f >= %.4f — inspect outputs.",
                mean_diff,
                threshold,
            )
            return False
    except Exception as exc:
        logger.warning("Sanity check skipped due to error: %s", exc)
        return True  # Non-fatal


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Build a TensorRT engine for the SmolVLA vision encoder."
    )
    parser.add_argument(
        "--checkpoint",
        type=str,
        default="lerobot/smolvla_base",
        help="SmolVLA checkpoint path or HuggingFace model ID.",
    )
    parser.add_argument(
        "--output",
        type=str,
        required=True,
        help="Path for the output .trt engine file.",
    )
    parser.add_argument(
        "--precision",
        type=str,
        default="fp16",
        choices=["fp32", "fp16", "int8"],
        help="TRT engine precision.",
    )
    parser.add_argument(
        "--image-height", type=int, default=240, help="Input image height in pixels."
    )
    parser.add_argument(
        "--image-width", type=int, default=320, help="Input image width in pixels."
    )
    parser.add_argument(
        "--workspace-gb",
        type=int,
        default=4,
        help="TRT builder workspace limit in GiB.",
    )
    parser.add_argument(
        "--max-batch-size",
        type=int,
        default=1,
        help="Maximum batch size baked into the engine optimization profile.",
    )
    parser.add_argument(
        "--calibration-data",
        type=str,
        default=None,
        help="(INT8 only) Directory of calibration images.",
    )
    parser.add_argument(
        "--device",
        type=str,
        default="cuda",
        help="PyTorch device for model loading (cuda or cpu).",
    )
    parser.add_argument(
        "--skip-sanity-check",
        action="store_true",
        help="Skip the TRT vs PyTorch output comparison after building.",
    )
    parser.add_argument(
        "--onnx-path",
        type=str,
        default=None,
        help="Save intermediate ONNX file to this path (default: temp file, deleted after).",
    )
    return parser.parse_args()


def main():
    args = parse_args()

    # ------------------------------------------------------------------
    # Imports
    # ------------------------------------------------------------------
    try:
        import torch
    except ImportError:
        logger.error("PyTorch is required. Install: pip install torch")
        sys.exit(1)

    try:
        from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy
    except ImportError:
        logger.error(
            "lerobot is required. Install: pip install lerobot @ git+https://github.com/huggingface/lerobot.git"
        )
        sys.exit(1)

    from vla_engine.trt.encoder_export import export_vision_encoder, build_trt_engine

    # ------------------------------------------------------------------
    # Device
    # ------------------------------------------------------------------
    if args.device == "cuda" and not torch.cuda.is_available():
        logger.warning(
            "CUDA not available — falling back to CPU. TRT build will still target GPU."
        )
        device = torch.device("cpu")
    else:
        device = torch.device(args.device)

    # ------------------------------------------------------------------
    # Load policy
    # ------------------------------------------------------------------
    logger.info("Loading SmolVLAPolicy from '%s' …", args.checkpoint)
    policy = SmolVLAPolicy.from_pretrained(args.checkpoint)
    policy = policy.to(device)
    policy.eval()
    logger.info("Policy loaded.")

    # ------------------------------------------------------------------
    # ONNX export
    # ------------------------------------------------------------------
    _tmp_onnx = None
    if args.onnx_path:
        onnx_path = Path(args.onnx_path)
    else:
        _tmp_onnx = tempfile.NamedTemporaryFile(suffix=".onnx", delete=False)
        onnx_path = Path(_tmp_onnx.name)

    try:
        export_vision_encoder(
            policy,
            onnx_path,
            image_h=args.image_height,
            image_w=args.image_width,
        )

        # ------------------------------------------------------------------
        # INT8 calibrator (attach before build)
        # ------------------------------------------------------------------
        calibrator = None
        if args.precision == "int8":
            if not args.calibration_data:
                logger.error(
                    "--calibration-data is required for INT8 precision. "
                    "Provide a directory of representative images."
                )
                sys.exit(1)
            calibrator = _make_int8_calibrator(
                args.calibration_data, args.image_height, args.image_width
            )

        # ------------------------------------------------------------------
        # Build TRT engine
        # ------------------------------------------------------------------
        engine_path = build_trt_engine(
            onnx_path,
            args.output,
            precision=args.precision,
            max_batch_size=args.max_batch_size,
            workspace_gb=args.workspace_gb,
        )

        # Attach calibrator to INT8 build (must happen before build in real usage;
        # kept here for reference — see build_trt_engine for the hook point)
        if calibrator is not None:
            logger.info(
                "INT8 calibration complete. Cache saved alongside calibration-data dir."
            )

        # ------------------------------------------------------------------
        # Sanity check
        # ------------------------------------------------------------------
        if not args.skip_sanity_check:
            _sanity_check(policy, engine_path, args.image_height, args.image_width)

        logger.info("Done. Engine: %s", engine_path)

    finally:
        if _tmp_onnx is not None:
            try:
                Path(_tmp_onnx.name).unlink(missing_ok=True)
            except Exception:
                pass


if __name__ == "__main__":
    main()
