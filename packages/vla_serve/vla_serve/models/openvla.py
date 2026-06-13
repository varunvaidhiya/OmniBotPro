"""OpenVLA model implementation for vla_serve."""

import re
import logging
from typing import Any, Dict, List, Union

import numpy as np
from PIL import Image

from .base import VLAModel

logger = logging.getLogger(__name__)


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

        logger.info("Loading OpenVLA from %s on %s...", model_path, self._device)
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

        logger.info("OpenVLA loaded.")

    @staticmethod
    def _parse_action_vector(text: str) -> List[float]:
        """
        Extract an action vector from OpenVLA's generated text.

        OpenVLA models output action tokens that decode to numeric text.
        The typical format is a sequence of numbers (space/comma separated)
        representing the action dimensions.  We extract the *last* contiguous
        group of numbers found in the text so that preceding instruction text
        is ignored.
        """
        matches = list(re.finditer(r"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", text))
        if not matches:
            logger.warning(
                "Could not parse action vector from generated text: %r", text
            )
            return []
        return [float(m.group()) for m in matches]

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
        action_vector = self._parse_action_vector(text)
        return {"vector": action_vector, "raw_output": text}
