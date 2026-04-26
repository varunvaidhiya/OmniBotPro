"""OpenVLA model implementation for vla_serve."""

from typing import Any, Dict, List, Union

import numpy as np
from PIL import Image

from .base import VLAModel


class OpenVLAModel(VLAModel):
    """
    Wrapper for OpenVLA models hosted on HuggingFace
    (e.g., openvla/openvla-7b).

    Requires:
        pip install vla-serve[openvla]
        # i.e.: torch transformers accelerate bitsandbytes
    """

    def __init__(self) -> None:
        self.model = None
        self.processor = None
        self._device = self._detect_device()

    @staticmethod
    def _detect_device() -> str:
        try:
            import torch

            return "cuda" if torch.cuda.is_available() else "cpu"
        except ImportError:
            return "cpu"

    def load_model(
        self,
        model_path: str = "openvla/openvla-7b",
        load_in_4bit: bool = False,
        **kwargs,
    ) -> None:
        import torch
        from transformers import AutoModelForVision2Seq, AutoProcessor

        print(f"Loading OpenVLA from {model_path} on {self._device}...")
        self.processor = AutoProcessor.from_pretrained(
            model_path, trust_remote_code=True
        )

        model_kwargs: Dict[str, Any] = {
            "trust_remote_code": True,
            "torch_dtype": (
                torch.bfloat16 if self._device == "cuda" else torch.float32
            ),
            "low_cpu_mem_usage": True,
        }
        if load_in_4bit:
            model_kwargs["load_in_4bit"] = True

        self.model = AutoModelForVision2Seq.from_pretrained(model_path, **model_kwargs)

        if not load_in_4bit:
            self.model.to(self._device)

        print("OpenVLA loaded.")

    def predict_action(
        self,
        image: Union[Image.Image, np.ndarray],
        instruction: str,
        max_new_tokens: int = 128,
        **kwargs,
    ) -> Union[List[float], Dict[str, Any]]:
        if self.model is None:
            raise RuntimeError("Model not loaded. Call load_model() first.")

        import torch

        if isinstance(image, np.ndarray):
            image = Image.fromarray(image)

        inputs = self.processor(text=instruction, images=image, return_tensors="pt").to(
            self._device
        )

        with torch.inference_mode():
            generated_ids = self.model.generate(
                **inputs,
                max_new_tokens=max_new_tokens,
                do_sample=False,
            )

        text = self.processor.batch_decode(generated_ids, skip_special_tokens=True)[0]
        return {"raw_output": text}
