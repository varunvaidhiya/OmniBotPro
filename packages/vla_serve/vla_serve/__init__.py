"""vla_serve — Model-agnostic VLA inference REST server."""

from .models.base import VLAModel
from .models.openvla import OpenVLAModel

__version__ = "1.0.0"
__all__ = ["VLAModel", "OpenVLAModel"]
