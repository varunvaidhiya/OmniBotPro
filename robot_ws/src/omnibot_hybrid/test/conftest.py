"""Shared fixtures for omnibot_hybrid tests.

rclpy is initialised once per test session and shut down at the end.
Both test_cmd_vel_mux.py and test_mission_planner_parser.py share this
context so that the init/shutdown pair is never called twice in the
same process, which can cause FastDDS re-initialisation failures.
"""

import pytest
import rclpy


@pytest.fixture(scope="session", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()
