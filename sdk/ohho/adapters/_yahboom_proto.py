"""Yahboom Rosmaster serial protocol — vendored, pure-stdlib codec.

Source of truth: ``packages/yahboom_ros2/yahboom_ros2/protocol.py`` (kept in
sync). Vendored here so the ``ohho-os`` SDK stays self-contained and the `base`
install needs no third-party packages — the adapter only needs pyserial (the
`[serial]` extra) to talk to a real port.

TX (host → board):  [0xFF, 0xFC, LEN, FUNC, PAYLOAD…, CHECKSUM]
    LEN      = 1(ID) + 1(LEN) + 1(FUNC) + N(payload)
    CHECKSUM = (sum(all bytes) + 5) & 0xFF      # 5 = 257 - 0xFC
RX (board → host):  [0xFF, 0xFB, LEN, TYPE, PAYLOAD…, CS]
    CS       = sum(LEN, TYPE, PAYLOAD…) & 0xFF
"""

from __future__ import annotations

import math
import struct
from dataclasses import dataclass
from typing import List, Optional, Sequence, Tuple

HEAD_TX: int = 0xFF
DEVICE_ID: int = 0xFC
HEAD_RX: int = 0xFB
_COMPLEMENT: int = 257 - DEVICE_ID  # = 5

# TX function codes
FUNC_BEEP: int = 0x02
FUNC_MOTOR: int = 0x10
FUNC_MOTION: int = 0x12
FUNC_SET_CAR_TYPE: int = 0x15

# RX response type codes
TYPE_VELOCITY: int = 0x0C
TYPE_ACCEL: int = 0x61
TYPE_GYRO: int = 0x62
TYPE_ATTITUDE: int = 0x63

# Car type constants
CAR_TYPE_MECANUM_X3: int = 1


# ── TX ─────────────────────────────────────────────────────────────────────────
def _checksum(packet: Sequence[int]) -> int:
    return (sum(packet) + _COMPLEMENT) & 0xFF


def build_packet(func_id: int, payload: Sequence[int]) -> bytes:
    length_val = 1 + 1 + 1 + len(payload)  # ID + LEN + FUNC + payload
    packet = [HEAD_TX, DEVICE_ID, length_val, func_id] + list(payload)
    packet.append(_checksum(packet))
    return bytes(packet)


def packet_set_car_type(car_type: int = CAR_TYPE_MECANUM_X3) -> bytes:
    return build_packet(FUNC_SET_CAR_TYPE, struct.pack("<b", car_type))


def packet_beep(duration_ms: int) -> bytes:
    return build_packet(FUNC_BEEP, struct.pack("<h", int(duration_ms)))


def packet_motion(
    vx: float, vy: float, vz: float, car_type: int = CAR_TYPE_MECANUM_X3
) -> bytes:
    """Holonomic motion. vx (m/s fwd), vy (m/s left), vz (rad/s CCW)."""
    payload = struct.pack(
        "<bhhh", car_type, int(vx * 1000), int(vy * 1000), int(vz * 1000)
    )
    return build_packet(FUNC_MOTION, payload)


# ── RX ─────────────────────────────────────────────────────────────────────────
@dataclass
class VelocityPacket:
    vx: float  # m/s
    vy: float  # m/s
    vz: float  # rad/s


@dataclass
class ImuAccelPacket:
    ax: float
    ay: float
    az: float


@dataclass
class ImuGyroPacket:
    gx: float
    gy: float
    gz: float


@dataclass
class ImuAttitudePacket:
    roll: float
    pitch: float
    yaw: float


RxPacket = VelocityPacket | ImuAccelPacket | ImuGyroPacket | ImuAttitudePacket


def _decode(pkt: bytes) -> Optional[RxPacket]:
    """Decode one validated RX packet (header + checksum already checked)."""
    pkt_type = pkt[3]
    payload = pkt[4:-1]
    if pkt_type == TYPE_VELOCITY and len(payload) >= 6:
        vx, vy, vz = struct.unpack_from("<hhh", payload)
        return VelocityPacket(vx / 1000.0, vy / 1000.0, vz / 1000.0)
    if pkt_type == TYPE_ACCEL and len(payload) >= 6:
        ax, ay, az = struct.unpack_from("<hhh", payload)
        return ImuAccelPacket(
            ax / 1000.0 * 9.81, ay / 1000.0 * 9.81, az / 1000.0 * 9.81
        )
    if pkt_type == TYPE_GYRO and len(payload) >= 6:
        gx, gy, gz = struct.unpack_from("<hhh", payload)
        return ImuGyroPacket(gx / 1000.0, gy / 1000.0, gz / 1000.0)
    if pkt_type == TYPE_ATTITUDE and len(payload) >= 6:
        r, p, y = struct.unpack_from("<hhh", payload)
        d = math.pi / 180.0 / 100.0
        return ImuAttitudePacket(r * d, p * d, y * d)
    return None


def parse_stream(data: bytes) -> Tuple[List[RxPacket], int]:
    """Parse complete RX packets from a streaming buffer.

    Returns ``(packets, consumed)`` — the caller should keep ``data[consumed:]``
    as the (possibly partial) remainder for the next read.
    """
    results: List[RxPacket] = []
    idx = 0
    n = len(data)
    while idx + 3 <= n:
        if data[idx] != HEAD_TX or data[idx + 1] != HEAD_RX:
            idx += 1
            continue
        length = data[idx + 2]
        total = 2 + length
        if idx + total > n:
            break  # incomplete — keep from idx
        pkt = data[idx : idx + total]
        if (sum(pkt[2:-1]) & 0xFF) != pkt[-1]:
            idx += 1  # bad checksum; resync
            continue
        parsed = _decode(pkt)
        if parsed is not None:
            results.append(parsed)
        idx += total
    return results, idx


def parse_rx_buffer(data: bytes) -> List[Tuple[int, RxPacket]]:
    """Compatibility helper: parse a full buffer, returning (offset, packet)."""
    out: List[Tuple[int, RxPacket]] = []
    idx = 0
    n = len(data)
    while idx + 3 <= n:
        if data[idx] != HEAD_TX or data[idx + 1] != HEAD_RX:
            idx += 1
            continue
        total = 2 + data[idx + 2]
        if idx + total > n:
            break
        pkt = data[idx : idx + total]
        if (sum(pkt[2:-1]) & 0xFF) == pkt[-1]:
            parsed = _decode(pkt)
            if parsed is not None:
                out.append((idx, parsed))
            idx += total
        else:
            idx += 1
    return out
