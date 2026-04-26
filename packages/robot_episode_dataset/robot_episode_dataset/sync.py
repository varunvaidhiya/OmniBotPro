"""
robot_episode_dataset.sync
~~~~~~~~~~~~~~~~~~~~~~~~~~
Timestamp synchronization for multi-modal sensor data.

No ROS dependency — works on raw timestamp arrays from any source.
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional, Set, Tuple

import numpy as np


class TopicSynchronizer:
    """
    Synchronize N data streams to a common set of target timestamps using
    nearest-neighbour matching with a configurable tolerance window.

    Example::

        sync = TopicSynchronizer(target_fps=10.0, sync_tolerance=0.05)
        frames = sync.synchronize({
            'camera_front': [(ts_ns, img), ...],
            'camera_wrist': [(ts_ns, img), ...],
            'state':        [(ts_ns, state_array), ...],
            'action':       [(ts_ns, action_array), ...],
        })
        # frames: list of dicts, each containing all modalities at one timestep
    """

    def __init__(
        self,
        target_fps: float = 10.0,
        sync_tolerance: float = 0.05,
        required_keys: Optional[Set[str]] = None,
    ) -> None:
        """
        Args:
            target_fps:     Output frequency in Hz.
            sync_tolerance: Max allowed time gap for nearest-neighbour match (s).
            required_keys:  Set of stream keys that MUST be present for a frame
                            to be included.  Defaults to all provided streams.
        """
        self.target_fps = target_fps
        self.sync_tolerance = sync_tolerance
        self.required_keys = required_keys

    def synchronize(
        self,
        data_streams: Dict[str, List[Tuple[int, Any]]],
    ) -> List[Dict[str, Any]]:
        """
        Synchronize multiple data streams to common timestamps.

        Args:
            data_streams: Mapping from stream name to list of
                          (timestamp_nanoseconds, value) tuples.

        Returns:
            List of synchronized frames.  Each frame is a dict with
            stream names as keys plus a 'timestamp' float key (seconds).
        """
        # Convert nanoseconds → seconds
        streams_sec: Dict[str, Tuple[np.ndarray, list]] = {}
        for key, data in data_streams.items():
            if not data:
                continue
            ts = np.array([t * 1e-9 for t, _ in data], dtype=np.float64)
            vals = [v for _, v in data]
            streams_sec[key] = (ts, vals)

        if not streams_sec:
            return []

        start_t = max(ts[0] for ts, _ in streams_sec.values())
        end_t = min(ts[-1] for ts, _ in streams_sec.values())

        if end_t <= start_t:
            return []

        dt = 1.0 / self.target_fps
        target_ts = np.arange(start_t, end_t, dt)

        required = self.required_keys or set(streams_sec.keys())
        frames: List[Dict[str, Any]] = []

        for t in target_ts:
            frame: Dict[str, Any] = {"timestamp": float(t)}

            for key, (timestamps, values) in streams_sec.items():
                idx = int(np.argmin(np.abs(timestamps - t)))
                if abs(timestamps[idx] - t) <= self.sync_tolerance:
                    frame[key] = values[idx]

            if required.issubset(frame.keys()):
                frames.append(frame)

        return frames
