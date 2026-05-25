"""Shared fixtures for omnibot_ota tests."""

import pytest
import rclpy


@pytest.fixture(scope="session", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()
