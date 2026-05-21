"""Policy model registry for OmniBot visuomotor training and inference.

Quick start
-----------
    from lerobot_engine.models import make_policy, list_models

    print(list_models())          # ['act', 'diffusion', 'openvla', 'smolvla']

    adapter = make_policy("smolvla", checkpoint="lerobot/smolvla_base")
    adapter.reset()
    action = adapter.select_action(obs_dict)   # np.ndarray (9,)
"""

from .base import PolicyAdapter
from .registry import register, make_policy, list_models

# Import adapters so they self-register via @register(...)
from . import smolvla, act, diffusion, openvla  # noqa: F401

__all__ = ["PolicyAdapter", "register", "make_policy", "list_models"]
