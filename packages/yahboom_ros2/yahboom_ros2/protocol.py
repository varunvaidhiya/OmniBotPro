"""
yahboom_ros2.protocol
~~~~~~~~~~~~~~~~~~~~~
Pure-Python encoder/decoder for the Yahboom ROS Robot Expansion Board
serial protocol (Rosmaster series).

This module has **no ROS dependency** and can be used standalone for
hardware testing, firmware development, or unit tests.

Packet format (TX — host → board):
    [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]

    LEN      = 1(ID) + 1(LEN) + 1(FUNC) + N(payload bytes)
    CHECKSUM = (sum(all_packet_bytes) + 5) & 0xFF   # 5 = 257 - 0xFC

Packet format (RX — board → host):
    [0xFF, 0xFB, LEN, TYPE, PAYLOAD..., CS]

    CS = sum(LEN, TYPE, PAYLOAD...) & 0xFF

Function codes (TX):
    FUNC_BEEP         = 0x02   buzzer control (int16 duration ms)
    FUNC_MOTOR        = 0x10   direct wheel PWM (4 × int16)
    FUNC_MOTION       = 0x12   holonomic motion (CAR_TYPE b + vx/vy/vz int16 × 1000)
    FUNC_SET_CAR_TYPE = 0x15   set robot type (X3 mecanum = 1)

Response types (RX):
    TYPE_VELOCITY     = 0x0C   vx, vy, vz (int16, ×1000 mm/s or mrad/s)
    TYPE_ACCEL        = 0x61   accelerometer (int16, mg → multiply by 9.81/1000)
    TYPE_GYRO         = 0x62   gyroscope (int16, mrad/s)
    TYPE_ATTITUDE     = 0x63   Euler angles (int16, 0.01 deg)
"""

from __future__ import annotations

import struct
from dataclasses import dataclass
from typing import List, Optional, Sequence, Tuple

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

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


# ---------------------------------------------------------------------------
# TX helpers
# ---------------------------------------------------------------------------


def _checksum(packet: Sequence[int]) -> int:
    """Compute Yahboom TX checksum: (sum(all bytes) + 5) & 0xFF."""
    return (sum(packet) + _COMPLEMENT) & 0xFF


def build_packet(func_id: int, payload: Sequence[int]) -> bytes:
    """Build a complete TX packet including header and checksum."""
    length_val = 1 + 1 + 1 + len(payload)  # ID + LEN + FUNC + payload
    packet = [HEAD_TX, DEVICE_ID, length_val, func_id] + list(payload)
    packet.append(_checksum(packet))
    return bytes(packet)


def packet_set_car_type(car_type: int = CAR_TYPE_MECANUM_X3) -> bytes:
    """Build SET_CAR_TYPE packet. Call 5× at startup with 50 ms gaps."""
    return build_packet(FUNC_SET_CAR_TYPE, struct.pack("<b", car_type))


def packet_beep(duration_ms: int) -> bytes:
    """Build BEEP packet. duration_ms=0 stops, >0 beeps for that many ms."""
    return build_packet(FUNC_BEEP, struct.pack("<h", int(duration_ms)))


def packet_motion(
    vx: float, vy: float, vz: float, car_type: int = CAR_TYPE_MECANUM_X3
) -> bytes:
    """
    Build MOTION packet (holonomic).

    Args:
        vx:  Forward velocity  (m/s, positive = forward)
        vy:  Lateral velocity  (m/s, positive = left)
        vz:  Angular velocity  (rad/s, positive = CCW)
        car_type: Board car-type constant (default: 1 = Mecanum X3)
    """
    payload = struct.pack(
        "<bhhh",
        car_type,
        int(vx * 1000),
        int(vy * 1000),
        int(vz * 1000),
    )
    return build_packet(FUNC_MOTION, payload)


def packet_motor(fl: int, fr: int, bl: int, br: int) -> bytes:
    """
    Build direct MOTOR packet (PWM values, -255 to +255).

    Args:
        fl, fr, bl, br: Front-left, Front-right, Back-left, Back-right PWM.
    """
    payload = struct.pack("<hhhh", fl, fr, bl, br)
    return build_packet(FUNC_MOTOR, payload)


# ---------------------------------------------------------------------------
# RX parsing
# ---------------------------------------------------------------------------


@dataclass
class VelocityPacket:
    vx: float  # m/s
    vy: float  # m/s
    vz: float  # rad/s


@dataclass
class ImuAccelPacket:
    ax: float  # m/s²
    ay: float  # m/s²
    az: float  # m/s²


@dataclass
class ImuGyroPacket:
    gx: float  # rad/s
    gy: float  # rad/s
    gz: float  # rad/s


@dataclass
class ImuAttitudePacket:
    roll: float  # rad
    pitch: float  # rad
    yaw: float  # rad


RxPacket = VelocityPacket | ImuAccelPacket | ImuGyroPacket | ImuAttitudePacket


def parse_rx_buffer(data: bytes) -> List[Tuple[int, RxPacket]]:
    """
    Parse all complete RX packets from a raw byte buffer.

    Returns a list of (byte_offset, parsed_packet) tuples so the caller
    can track where each packet started.
    """
    results: List[Tuple[int, RxPacket]] = []
    idx = 0
    n = len(data)

    while idx < n - 2:
        if data[idx] != HEAD_TX or data[idx + 1] != HEAD_RX:
            idx += 1
            continue
        if idx + 3 > n:
            break

        length = data[idx + 2]
        total = 2 + length
        if idx + total > n:
            break  # incomplete — wait for more data

        pkt = data[idx : idx + total]
        pkt_type = pkt[3]
        payload = pkt[4:-1]

        parsed: Optional[RxPacket] = None

        if pkt_type == TYPE_VELOCITY and len(payload) >= 6:
            vx_raw, vy_raw, vz_raw = struct.unpack_from("<hhh", payload)
            parsed = VelocityPacket(
                vx=vx_raw / 1000.0,
                vy=vy_raw / 1000.0,
                vz=vz_raw / 1000.0,
            )
        elif pkt_type == TYPE_ACCEL and len(payload) >= 6:
            ax, ay, az = struct.unpack_from("<hhh", payload)
            parsed = ImuAccelPacket(
                ax=ax / 1000.0 * 9.81,
                ay=ay / 1000.0 * 9.81,
                az=az / 1000.0 * 9.81,
            )
        elif pkt_type == TYPE_GYRO and len(payload) >= 6:
            gx, gy, gz = struct.unpack_from("<hhh", payload)
            parsed = ImuGyroPacket(
                gx=gx / 1000.0,
                gy=gy / 1000.0,
                gz=gz / 1000.0,
            )
        elif pkt_type == TYPE_ATTITUDE and len(payload) >= 6:
            import math

            r, p, y = struct.unpack_from("<hhh", payload)
            parsed = ImuAttitudePacket(
                roll=r / 100.0 * math.pi / 180.0,
                pitch=p / 100.0 * math.pi / 180.0,
                yaw=y / 100.0 * math.pi / 180.0,
            )

        if parsed is not None:
            results.append((idx, parsed))

        idx += total

    return results
