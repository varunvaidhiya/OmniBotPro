"""Timestamp synchronization for multi-modal data"""

import numpy as np
from typing import Dict, List, Tuple


class TopicSynchronizer:
    """Synchronize multi-modal sensor data by timestamp"""

    def __init__(self, target_fps: float = 10.0, sync_tolerance: float = 0.05):
        """
        Args:
            target_fps: Output frequency in Hz
            sync_tolerance: Max time difference for matching (seconds)
        """
        self.target_fps = target_fps
        self.sync_tolerance = sync_tolerance

    def synchronize(self, data_streams: Dict[str, List[Tuple]]) -> List[Dict]:
        """
        Synchronize multiple data streams to common timestamps.

        Args:
            data_streams: {
                'camera_front': [(ts_ns, img), ...],
                'camera_wrist': [(ts_ns, img), ...],
                'state': [(ts_ns, state), ...],
                'action': [(ts_ns, action), ...]
            }

        Returns:
            List of synchronized frames, each containing all modalities
        """
        # Convert nanoseconds to seconds
        data_sec = {}
        for key, data in data_streams.items():
            if len(data) > 0:
                timestamps = np.array([ts * 1e-9 for ts, _ in data])
                values = [val for _, val in data]
                data_sec[key] = (timestamps, values)

        # Find common time range
        start_time = max(ts[0] for ts, _ in data_sec.values())
        end_time = min(ts[-1] for ts, _ in data_sec.values())

        # Generate target timestamps
        dt = 1.0 / self.target_fps
        target_timestamps = np.arange(start_time, end_time, dt)

        print(
            f"Synchronizing {len(target_timestamps)} frames from "
            f"{start_time:.2f}s to {end_time:.2f}s"
        )

        # Synchronize each stream
        synchronized = []

        for target_ts in target_timestamps:
            frame = {"timestamp": target_ts}

            for key, (timestamps, values) in data_sec.items():
                # Find nearest data point
                idx = np.argmin(np.abs(timestamps - target_ts))

                # Check if within tolerance
                if abs(timestamps[idx] - target_ts) > self.sync_tolerance:
                    print(
                        f"Warning: No match within tolerance for {key} "
                        f"at t={target_ts:.3f}s"
                    )
                    continue

                frame[key] = values[idx]

            # Only add frame if all required data present
            required_keys = set(data_sec.keys())
            if required_keys.issubset(frame.keys()):
                synchronized.append(frame)

        print(f"Successfully synchronized {len(synchronized)} frames")
        return synchronized
