"""Data collection — record episodes from a live robot to LeRobot format.

``Recorder`` wraps a :class:`~ohho.robot.Robot`, intercepts its commands to
capture actions, polls telemetry for state, and writes episodes to disk in
LeRobot v2.0 layout (Parquet + meta JSON). The same dataset feeds
:mod:`ohho.train` and :mod:`ohho.serve`.

Heavy deps (``pyarrow`` for Parquet) are lazy: without the ``[data]`` extra the
writer falls back to JSON Lines so the record→inspect loop still works.
"""

from __future__ import annotations

from .recorder import Recorder, Episode

__all__ = ["Recorder", "Episode"]
