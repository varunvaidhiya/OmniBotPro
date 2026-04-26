import torch
from PIL import Image
from transformers import AutoModelForVision2Seq, AutoProcessor
from .base import VLAModel
from typing import Union, List, Dict, Any
import numpy as np


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
        print(f"Loading OpenVLA model from {model_path} on {self.device}...")

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

        print("Model loaded successfully.")

    def predict_action(
        self, image: Union[Image.Image, np.ndarray], instruction: str, **kwargs
    ) -> Union[List[float], Dict[str, Any]]:
        """
        Run inference using OpenVLA.
        """
        if self.model is None:
            raise RuntimeError("Model not loaded. Call load_model() first.")

        if isinstance(image, np.ndarray):
            image = Image.fromarray(image)

        inputs = self.processor(text=instruction, images=image, return_tensors="pt").to(
            self.device
        )

        with torch.inference_mode():
            # OpenVLA specific generation
            # TODO: Fine-tune generation parameters for specific robot action space if needed
            generated_ids = self.model.generate(
                **inputs, max_new_tokens=128, do_sample=False
            )

        generated_text = self.processor.batch_decode(
            generated_ids, skip_special_tokens=True
        )[0]

        # Post-process the text to extract action vector if the model outputs raw text tokens
        # For OpenVLA, it typically outputs a specific format or we might need to parse it.
        # Assuming standard OpenVLA behavior which might need adaptation for this specific robot's joint space.

        # Placeholder parsing logic - in a real scenario, we'd parse the action tokens
        return {"raw_output": generated_text}
