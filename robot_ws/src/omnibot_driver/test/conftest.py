"""Shared fixtures for omnibot_driver tests."""

import pytest
from unittest.mock import MagicMock, patch


@pytest.fixture()
def mock_serial(monkeypatch):
    """Return a mock serial.Serial instance.

    The mock is patched into the serial module so that YahboomControllerNode
    (and any other code that calls serial.Serial) receives it transparently.
    """
    ser = MagicMock()
    ser.is_open = False
    ser.in_waiting = 0
    ser.read.return_value = b""

    with patch("serial.Serial", return_value=ser):
        yield ser
