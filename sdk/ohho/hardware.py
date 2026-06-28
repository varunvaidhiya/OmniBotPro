"""Hardware / device helpers.

Light by default — torch is imported lazily and only when a caller asks to
resolve a compute device. This keeps the `base` install dependency-free.
"""

from __future__ import annotations


def resolve_device(prefer: str = "auto") -> str:
    """Resolve a compute device string.

    ``prefer="auto"`` picks cuda → mps → cpu based on what's importable; any other
    value is returned unchanged so callers can force a device.
    """
    if prefer != "auto":
        return prefer
    try:  # torch is optional; absence simply means cpu
        import torch  # type: ignore

        if torch.cuda.is_available():
            return "cuda"
        mps = getattr(torch.backends, "mps", None)
        if mps is not None and mps.is_available():
            return "mps"
    except Exception:
        pass
    return "cpu"
