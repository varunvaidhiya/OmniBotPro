from abc import ABC, abstractmethod
from typing import Union, List, Dict, Any
import numpy as np
from PIL import Image


class VLAModel(ABC):
    """
    Abstract Base Class for Vision-Language-Action models.
    """

    @abstractmethod
    def load_model(self, model_path: str, **kwargs) -> None:
        """
        Load the model from a specific path or huggingface hub ID.
        """
        pass

    @abstractmethod
    def predict_action(
        self, image: Union[Image.Image, np.ndarray], instruction: str, **kwargs
    ) -> Union[List[float], Dict[str, Any]]:
        """
        Given an image and natural language instruction, predict the robot action.

        Args:
            image: PIL Image or Numpy array (RGB).
            instruction: Text prompt.

        Returns:
            Action vector (e.g., joint velocities) or dictionary.
        """
        pass
