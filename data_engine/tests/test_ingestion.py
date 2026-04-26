"""Test Ingestion Logic"""

import numpy as np
from data_engine.ingestion.sync_topics import TopicSynchronizer


def test_synchronizer():
    """Test timestamp synchronization"""

    sync = TopicSynchronizer(target_fps=10.0, sync_tolerance=0.05)

    # Create synthetic data
    # 3 streams: camera (30fps), state (50fps), action (10fps)
    duration = 2.0

    t_cam = np.arange(0, duration, 1 / 30.0)
    cam_data = [(int(t * 1e9), f"img_{i}") for i, t in enumerate(t_cam)]

    t_state = np.arange(0, duration, 1 / 50.0)
    state_data = [(int(t * 1e9), f"state_{i}") for i, t in enumerate(t_state)]

    t_action = np.arange(0, duration, 1 / 10.0)
    action_data = [(int(t * 1e9), f"action_{i}") for i, t in enumerate(t_action)]

    streams = {"camera": cam_data, "state": state_data, "action": action_data}

    results = sync.synchronize(streams)

    # Check results
    assert len(results) > 0
    # Should get roughly 10fps * 2s = 20 frames
    assert abs(len(results) - 20) <= 2

    for frame in results:
        assert "camera" in frame
        assert "state" in frame
        assert "action" in frame
        assert "timestamp" in frame


if __name__ == "__main__":
    test_synchronizer()
