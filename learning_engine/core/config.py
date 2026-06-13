"""Config loading for the learning loop.

Configs are plain nested dicts (YAML on disk, JSON also accepted). Component
sections follow the convention::

    reward_engine:
      terms:
        - {name: goal_distance, weight: 1.0}
        - {name: collision, weight: 5.0, min_distance_m: 0.25}

where ``name`` selects a registry entry and the remaining keys are passed to
the constructor.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict

try:
    import yaml  # type: ignore

    _HAS_YAML = True
except ImportError:  # pragma: no cover - exercised only without pyyaml
    _HAS_YAML = False


def load_config(path: str) -> Dict[str, Any]:
    p = Path(path).expanduser()
    text = p.read_text()
    if p.suffix in (".yaml", ".yml"):
        if not _HAS_YAML:
            raise RuntimeError(
                f"pyyaml is required to load {p}; `pip install pyyaml` or use JSON"
            )
        return yaml.safe_load(text) or {}
    return json.loads(text)


def build_from_spec(registry: Any, spec: Dict[str, Any]) -> Any:
    """Instantiate ``{"name": ..., **kwargs}`` from a registry."""
    kwargs = dict(spec)
    name = kwargs.pop("name")
    return registry.create(name, **kwargs)
