"""Shared fixtures for omnibot_driver tests."""

import struct
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


def build_rx_packet(pkt_type: int, payload: bytes) -> bytes:
    """Helper: build a valid Yahboom RX packet."""
    # Format: [0xFF, 0xFB, LEN, TYPE, PAYLOAD..., CS]
    # LEN = 3 + len(payload)  (covers LEN, TYPE, PAYLOAD, CS)
    length = 3 + len(payload)
    header = bytes([0xFF, 0xFB, length, pkt_type])
    body = header[2:] + payload  # LEN + TYPE + PAYLOAD
    checksum = sum(body) & 0xFF
    return header + payload + bytes([checksum])


def build_velocity_packet(vx_mms: int, vy_mms: int, vz_mrads: int) -> bytes:
    """Build a TYPE=0x0C velocity feedback packet."""
    payload = struct.pack("<hhh", vx_mms, vy_mms, vz_mrads)
    return build_rx_packet(0x0C, payload)
