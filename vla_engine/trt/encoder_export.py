"""TensorRT-accelerated vision encoder for visuomotor policies.

Provides a drop-in replacement for any policy's vision encoder that runs
through a pre-built TRT FP16 engine, giving a 2-3x speedup on the encoder
forward pass without changing any downstream model behaviour.

Works with any policy whose vision encoder can be located via the
find_vision_encoder() attribute-path search (SmolVLA, ACT-with-ViT, etc.).

Typical workflow
----------------
1. Build the engine once (offline):
       python -m vla_engine.trt.build_engine \\
           --checkpoint lerobot/smolvla_base \\
           --output engines/vision_fp16.trt

2. At runtime, patch the loaded policy:
       from vla_engine.trt import patch_policy_vision_encoder
       patch_policy_vision_encoder(policy, "engines/vision_fp16.trt")

The patch is transparent — policy.select_action() continues to work normally.
"""

from __future__ import annotations

import logging
import os
from pathlib import Path
from typing import Optional

import torch
import torch.nn as nn

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Optional TensorRT imports
# ---------------------------------------------------------------------------
try:
    import tensorrt as trt

    TRT_AVAILABLE = True
    TRT_LOGGER = trt.Logger(trt.Logger.WARNING)
except ImportError:
    TRT_AVAILABLE = False
    TRT_LOGGER = None

# ---------------------------------------------------------------------------
# Vision encoder discovery
# ---------------------------------------------------------------------------

# Candidate attribute paths for the vision encoder inside SmolVLA / SmolVLM.
# We try each in order and use the first that resolves to an nn.Module.
_VISION_ENCODER_PATHS = [
    # SmolVLM / SmolVLA (lerobot) — most likely
    "model.model.vision_model",
    "model.model.vision_tower",
    "model.vision_model",
    "model.vision_tower",
    "model.vision_encoder",
    # Generic fallbacks
    "vision_encoder",
    "vision_model",
    "vision_tower",
    "backbone",
]


def find_vision_encoder(policy: nn.Module) -> tuple[nn.Module, str]:
    """Return (encoder_module, dotted_attr_path) for the first matching path.

    Raises RuntimeError if no encoder can be found.
    """
    for path in _VISION_ENCODER_PATHS:
        obj = policy
        try:
            for attr in path.split("."):
                obj = getattr(obj, attr)
            if isinstance(obj, nn.Module):
                logger.info("Found vision encoder at policy.%s", path)
                return obj, path
        except AttributeError:
            continue

    # Last-resort: search named modules for anything with "vision" in its name
    for name, module in policy.named_modules():
        lower = name.lower()
        if any(k in lower for k in ("vision_model", "vision_encoder", "vision_tower")):
            if isinstance(module, nn.Module) and name:
                logger.info("Found vision encoder via named_modules scan: %s", name)
                return module, name

    raise RuntimeError(
        "Could not locate the vision encoder inside the policy. "
        "Add its attribute path to _VISION_ENCODER_PATHS in encoder_export.py."
    )


def _set_nested_attr(obj: object, dotted_path: str, value: object) -> None:
    """Set a nested attribute given a dotted path string."""
    parts = dotted_path.split(".")
    for attr in parts[:-1]:
        obj = getattr(obj, attr)
    setattr(obj, parts[-1], value)


# ---------------------------------------------------------------------------
# ONNX export
# ---------------------------------------------------------------------------


def export_vision_encoder(
    policy: nn.Module,
    output_path: str | os.PathLike,
    image_h: int = 240,
    image_w: int = 320,
    opset: int = 17,
) -> Path:
    """Export the SmolVLA vision encoder to ONNX.

    Args:
        policy: Loaded SmolVLAPolicy (on CPU or CUDA, eval mode).
        output_path: Destination .onnx file path.
        image_h: Input image height (must match training resolution).
        image_w: Input image width.
        opset: ONNX opset version.

    Returns:
        Path to the written ONNX file.
    """
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    encoder, _ = find_vision_encoder(policy)
    encoder = encoder.eval()

    device = next(encoder.parameters()).device
    dummy_input = torch.zeros(1, 3, image_h, image_w, device=device)

    logger.info(
        "Exporting vision encoder to ONNX: %s  (input %dx%d)",
        output_path,
        image_h,
        image_w,
    )

    with torch.no_grad():
        torch.onnx.export(
            encoder,
            dummy_input,
            str(output_path),
            opset_version=opset,
            input_names=["image"],
            output_names=["vision_features"],
            dynamic_axes={
                "image": {0: "batch"},
                "vision_features": {0: "batch"},
            },
            do_constant_folding=True,
        )

    logger.info("ONNX export complete: %s", output_path)
    return output_path


# ---------------------------------------------------------------------------
# TRT engine build
# ---------------------------------------------------------------------------


def build_trt_engine(
    onnx_path: str | os.PathLike,
    engine_path: str | os.PathLike,
    precision: str = "fp16",
    max_batch_size: int = 1,
    workspace_gb: int = 4,
) -> Path:
    """Build a TensorRT engine from an ONNX file.

    Args:
        onnx_path: Path to the .onnx file produced by export_vision_encoder().
        engine_path: Destination .trt engine file.
        precision: One of "fp32", "fp16", "int8" (INT8 requires a calibrator).
        max_batch_size: Maximum batch size baked into the engine profile.
        workspace_gb: Builder workspace limit in GiB.

    Returns:
        Path to the serialised engine file.
    """
    if not TRT_AVAILABLE:
        raise RuntimeError(
            "tensorrt is not installed. Install it with: pip install tensorrt"
        )

    onnx_path = Path(onnx_path)
    engine_path = Path(engine_path)
    engine_path.parent.mkdir(parents=True, exist_ok=True)

    builder = trt.Builder(TRT_LOGGER)
    network_flags = 1 << int(trt.NetworkDefinitionCreationFlag.EXPLICIT_BATCH)
    network = builder.create_network(network_flags)
    parser = trt.OnnxParser(network, TRT_LOGGER)

    with open(onnx_path, "rb") as f:
        if not parser.parse(f.read()):
            errors = [str(parser.get_error(i)) for i in range(parser.num_errors)]
            raise RuntimeError("ONNX parse failed:\n" + "\n".join(errors))

    config = builder.create_builder_config()
    config.set_memory_pool_limit(trt.MemoryPoolType.WORKSPACE, workspace_gb * (1 << 30))

    if precision == "fp16":
        if not builder.platform_has_fast_fp16:
            logger.warning("GPU does not support fast FP16; falling back to FP32.")
        else:
            config.set_flag(trt.BuilderFlag.FP16)
    elif precision == "int8":
        if not builder.platform_has_fast_int8:
            raise RuntimeError("GPU does not support INT8 inference.")
        config.set_flag(trt.BuilderFlag.INT8)
        # Calibrator must be set separately via config.int8_calibrator
        logger.warning(
            "INT8 selected but no calibrator attached. "
            "Set config.int8_calibrator before building, or use build_engine.py --calibrate."
        )

    # Dynamic shape profile: batch 1 fixed (common for robotics real-time control)
    profile = builder.create_optimization_profile()
    input_tensor = network.get_input(0)
    input_name = input_tensor.name
    _, c, h, w = input_tensor.shape  # (batch=-1, C, H, W)
    min_shape = (1, c, h, w)
    opt_shape = (1, c, h, w)
    max_shape = (max_batch_size, c, h, w)
    profile.set_shape(input_name, min_shape, opt_shape, max_shape)
    config.add_optimization_profile(profile)

    logger.info(
        "Building TRT engine (precision=%s) — this may take several minutes …",
        precision,
    )
    serialized = builder.build_serialized_network(network, config)
    if serialized is None:
        raise RuntimeError("TRT engine build failed. Check ONNX model and GPU support.")

    with open(engine_path, "wb") as f:
        f.write(serialized)

    logger.info("TRT engine saved: %s", engine_path)
    return engine_path


# ---------------------------------------------------------------------------
# TRT runtime wrapper
# ---------------------------------------------------------------------------


def load_trt_engine(engine_path: str | os.PathLike) -> "trt.ICudaEngine":
    """Deserialise and return a TRT engine from a .trt file."""
    if not TRT_AVAILABLE:
        raise RuntimeError("tensorrt is not installed.")

    engine_path = Path(engine_path)
    runtime = trt.Runtime(TRT_LOGGER)
    with open(engine_path, "rb") as f:
        engine = runtime.deserialize_cuda_engine(f.read())
    if engine is None:
        raise RuntimeError(f"Failed to deserialise TRT engine: {engine_path}")
    logger.info("TRT engine loaded: %s", engine_path)
    return engine


class TRTVisionEncoderModule(nn.Module):
    """Drop-in nn.Module replacement for a SmolVLA vision encoder.

    Runs inference through a pre-built TRT FP16/FP32 engine.
    Input/output tensors remain on GPU; no host copies occur during forward().

    Args:
        engine_path: Path to a .trt engine file built by build_trt_engine().
        output_dtype: Torch dtype to cast the output to (matches rest of model).
    """

    def __init__(
        self,
        engine_path: str | os.PathLike,
        output_dtype: torch.dtype = torch.float32,
    ):
        super().__init__()
        if not TRT_AVAILABLE:
            raise RuntimeError("tensorrt is not installed.")

        self.engine_path = Path(engine_path)
        self.output_dtype = output_dtype
        self._engine: Optional["trt.ICudaEngine"] = None
        self._context: Optional["trt.IExecutionContext"] = None
        self._stream = torch.cuda.Stream()

        # Lazy-init on first forward() so the module can be pickled / moved
        # across processes before the CUDA context is created.
        self._initialised = False

    # ------------------------------------------------------------------
    # Lazy TRT initialisation
    # ------------------------------------------------------------------

    def _init_trt(self):
        if self._initialised:
            return
        self._engine = load_trt_engine(self.engine_path)
        self._context = self._engine.create_execution_context()
        self._initialised = True

    # ------------------------------------------------------------------
    # Forward
    # ------------------------------------------------------------------

    def forward(self, pixel_values: torch.Tensor, **kwargs) -> torch.Tensor:
        """Run the TRT engine.

        Args:
            pixel_values: Float tensor (B, C, H, W) on CUDA.
            **kwargs: Ignored; accepted for API compatibility with HF vision models.

        Returns:
            Vision feature tensor (B, seq_len, hidden_dim) on the same device.
        """
        self._init_trt()

        pixel_values = pixel_values.contiguous().float()
        batch = pixel_values.shape[0]

        # Determine output shape from the engine binding
        self._context.set_input_shape(
            self._engine.get_tensor_name(0), tuple(pixel_values.shape)
        )
        out_shape = tuple(
            self._context.get_tensor_shape(self._engine.get_tensor_name(1))
        )
        out_shape = (batch,) + out_shape[1:]  # fix batch dim

        output = torch.empty(out_shape, dtype=torch.float32, device=pixel_values.device)

        with torch.cuda.stream(self._stream):
            self._context.set_tensor_address(
                self._engine.get_tensor_name(0), pixel_values.data_ptr()
            )
            self._context.set_tensor_address(
                self._engine.get_tensor_name(1), output.data_ptr()
            )
            self._context.execute_async_v3(stream_handle=self._stream.cuda_stream)

        torch.cuda.current_stream().wait_stream(self._stream)

        if self.output_dtype != torch.float32:
            output = output.to(self.output_dtype)

        return output

    def extra_repr(self) -> str:
        return f"engine={self.engine_path}, output_dtype={self.output_dtype}"


# ---------------------------------------------------------------------------
# Policy patch helper
# ---------------------------------------------------------------------------


def patch_policy_vision_encoder(
    policy: nn.Module,
    engine_path: str | os.PathLike,
    output_dtype: Optional[torch.dtype] = None,
) -> TRTVisionEncoderModule:
    """Replace the vision encoder inside *policy* with a TRT-backed module.

    The original PyTorch encoder is discarded from the module tree, freeing
    its VRAM. The TRT engine is loaded lazily on the first forward() call.

    Args:
        policy: A loaded SmolVLAPolicy in eval mode.
        engine_path: Path to the .trt engine file.
        output_dtype: Output dtype (defaults to the encoder's existing dtype).

    Returns:
        The TRTVisionEncoderModule that was installed.
    """
    original_encoder, attr_path = find_vision_encoder(policy)

    if output_dtype is None:
        try:
            output_dtype = next(original_encoder.parameters()).dtype
        except StopIteration:
            output_dtype = torch.float32

    trt_module = TRTVisionEncoderModule(engine_path, output_dtype=output_dtype)

    _set_nested_attr(policy, attr_path, trt_module)

    # Free original encoder weights from GPU memory
    original_encoder.cpu()
    del original_encoder

    logger.info(
        "Patched policy.%s → TRTVisionEncoderModule (engine=%s, dtype=%s)",
        attr_path,
        engine_path,
        output_dtype,
    )
    return trt_module
