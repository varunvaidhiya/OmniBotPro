import logging
import re
from typing import Union, List, Dict, Any

import numpy as np
import torch
from PIL import Image
from transformers import AutoModelForVision2Seq, AutoProcessor

from .base import VLAModel

logger = logging.getLogger(__name__)


class OpenVLAModel(VLAModel):
    """
    Wrapper for OpenVLA models (e.g., openvla/openvla-7b).
    """

    def __init__(self):
        self.model = None
        self.processor = None
        self.device = "cuda" if torch.cuda.is_available() else "cpu"

    def load_model(
        self,
        model_path: str = "openvla/openvla-7b",
        load_in_4bit: bool = False,
        **kwargs,
    ) -> None:
        """
        Load OpenVLA model and processor.

        Args:
            model_path: HuggingFace model ID or local path.
            load_in_4bit: Whether to load in 4-bit quantization (requires bitsandbytes).
        """
        logger.info("Loading OpenVLA model from %s on %s...", model_path, self.device)

        self.processor = AutoProcessor.from_pretrained(
            model_path, trust_remote_code=True
        )

        model_kwargs = {
            "trust_remote_code": True,
            "torch_dtype": torch.bfloat16 if self.device == "cuda" else torch.float32,
            "low_cpu_mem_usage": True,
        }

        if load_in_4bit:
            model_kwargs["load_in_4bit"] = True

        self.model = AutoModelForVision2Seq.from_pretrained(model_path, **model_kwargs)

        if not load_in_4bit:
            self.model.to(self.device)

        logger.info("Model loaded successfully.")

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
        self, image: Union[Image.Image, np.ndarray], instruction: str, **kwargs
    ) -> Union[List[float], Dict[str, Any]]:
        """
        Run inference using OpenVLA.

        Returns:
            dict with 'vector' (list of float action values) and
            'raw_output' (raw generated text).
        """
        if self.model is None:
            raise RuntimeError("Model not loaded. Call load_model() first.")

        if isinstance(image, np.ndarray):
            image = Image.fromarray(image)

        inputs = self.processor(text=instruction, images=image, return_tensors="pt").to(
            self.device
        )

        with torch.inference_mode():
            generated_ids = self.model.generate(
                **inputs, max_new_tokens=128, do_sample=False
            )

        generated_text = self.processor.batch_decode(
            generated_ids, skip_special_tokens=True
        )[0]

        action_vector = self._parse_action_vector(generated_text)
        return {"vector": action_vector, "raw_output": generated_text}
