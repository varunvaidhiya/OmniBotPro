"""Adapter errors (separate module so adapters can import it without cycling
through ``ohho.adapters.__init__``)."""

from __future__ import annotations


class AdapterUnavailable(RuntimeError):
    """Raised when a transport scheme is unknown, or when an adapter's runtime
    dependency (pyserial, cyclonedds, the Unitree SDK, …) isn't installed."""
