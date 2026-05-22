#!/usr/bin/env python3
"""
Yahboom 0x0E encoder packet decoder.

Run WITHOUT ROS. Sends STOP first, waits for wheels to settle, then
tests forward / rotate / strafe motions to identify which bytes in the
0x0E packet correspond to which wheel and in what units.

Usage:
    python3 decode_encoders.py              # /dev/ttyUSB0
    python3 decode_encoders.py /dev/ttyUSB1
"""

import sys
import time
import struct
import serial
import statistics

PORT = sys.argv[1] if len(sys.argv) > 1 else "/dev/ttyUSB0"
BAUD = 115200

HEAD_TX, DEVICE_ID, HEAD_RX = 0xFF, 0xFC, 0xFB


def _cksum(pkt):
    return (sum(pkt) + (257 - DEVICE_ID)) & 0xFF


def build(func, payload):
    pkt = [HEAD_TX, DEVICE_ID, 0, func] + list(payload)
    pkt[2] = len(pkt) - 1
    pkt.append(_cksum(pkt))
    return bytes(pkt)


def send(ser, pkt):
    ser.write(pkt)
    time.sleep(0.005)


PKT_STOP = build(0x12, struct.pack("<bhhh", 1, 0, 0, 0))
PKT_CAR_TYPE = build(0x15, struct.pack("<b", 1))


def motion(vx, vy, vz):
    return build(
        0x12, struct.pack("<bhhh", 1, int(vx * 1000), int(vy * 1000), int(vz * 1000))
    )


# ── read 0x0E packets from serial for `duration` seconds ────────────────────
def collect_0x0E(ser, duration):
    buf = b""
    pkts = []
    deadline = time.time() + duration
    while time.time() < deadline:
        chunk = ser.read(ser.in_waiting or 1)
        buf += chunk
        i = 0
        while i < len(buf) - 3:
            if buf[i] == HEAD_TX and buf[i + 1] == HEAD_RX:
                ln = buf[i + 2]
                tot = 2 + ln
                if i + tot > len(buf):
                    break
                pkt = buf[i : i + tot]
                pkt_type = pkt[3]
                payload = pkt[4:-1]
                if pkt_type == 0x0E and len(payload) >= 8:
                    pkts.append(payload)
                i += tot
            else:
                i += 1
        buf = buf[i:]
    return pkts


def stats(pkts, byte_pairs):
    """
    For each int16 field (list of (label, byte_offset) pairs),
    return mean of all collected packets.
    """
    if not pkts:
        return {}
    result = {}
    for label, offset in byte_pairs:
        vals = []
        for p in pkts:
            if len(p) >= offset + 2:
                v = struct.unpack_from("<h", p, offset)[0]
                vals.append(v)
        if vals:
            result[label] = (
                statistics.mean(vals),
                statistics.stdev(vals) if len(vals) > 1 else 0,
            )
    return result


def print_stats(label, s):
    print(f"\n  ── {label} ──")
    for k, (mean, std) in s.items():
        print(f"    {k:6s}: mean={mean:8.1f}  stdev={std:6.1f}")


# ── main ─────────────────────────────────────────────────────────────────────
print(f"\n{'=' * 60}")
print(f" Yahboom 0x0E Encoder Decoder  —  {PORT}")
print(f"{'=' * 60}")

ser = serial.Serial(port=PORT, baudrate=BAUD, timeout=0.1)

# ── Init ─────────────────────────────────────────────────────────────────────
print("\n[1] Stopping any residual motion ...")
for _ in range(5):
    send(ser, PKT_STOP)
    time.sleep(0.02)
time.sleep(0.3)
for _ in range(5):
    send(ser, PKT_CAR_TYPE)
    time.sleep(0.05)
time.sleep(0.3)
for _ in range(3):
    send(ser, PKT_STOP)
    time.sleep(0.02)
print("    Waiting 3 s for wheels to fully stop ...")
time.sleep(3.0)
ser.reset_input_buffer()

# Fields: label → byte offset in payload (each field = int16 LE)
# 18-byte payload → 9 possible int16 fields at offsets 0,2,4,6,8,10,12,14,16
FIELDS = [(f"b{i * 2:02d}", i * 2) for i in range(9)]

# ── Baseline (stationary) ─────────────────────────────────────────────────────
print("\n[2] Recording baseline (stationary, 3 s) ...")
pkts_rest = collect_0x0E(ser, 3.0)
s_rest = stats(pkts_rest, FIELDS)
print_stats(f"REST  ({len(pkts_rest)} pkts)", s_rest)
print(
    "\n  Raw sample:",
    " ".join(f"{b:02X}" for b in pkts_rest[0]) if pkts_rest else "none",
)


def run_motion_test(ser, label, vx, vy, vz, duration=1.5, settle=1.5):
    print(f"\n[*] {label} (vx={vx} vy={vy} vz={vz}) for {duration}s ...")
    ser.reset_input_buffer()
    t_end = time.time() + duration
    pkts = []
    while time.time() < t_end:
        send(ser, motion(vx, vy, vz))
        time.sleep(0.05)
        chunk = ser.read(ser.in_waiting or 0)
        buf = chunk
        i = 0
        while i < len(buf) - 3:
            if buf[i] == HEAD_TX and buf[i + 1] == HEAD_RX:
                ln = buf[i + 2]
                tot = 2 + ln
                if i + tot > len(buf):
                    break
                pkt = buf[i : i + tot]
                pkt_type = pkt[3]
                payload = pkt[4:-1]
                if pkt_type == 0x0E and len(payload) >= 8:
                    pkts.append(payload)
                i += tot
            else:
                i += 1

    # Stop and let settle
    for _ in range(5):
        send(ser, PKT_STOP)
        time.sleep(0.02)
    time.sleep(settle)

    s = stats(pkts, FIELDS)
    print_stats(f"{label} ({len(pkts)} pkts)", s)
    if pkts:
        print("  Raw sample:", " ".join(f"{b:02X}" for b in pkts[0]))
    return s, pkts


# ── Motion tests ──────────────────────────────────────────────────────────────
s_fwd, p_fwd = run_motion_test(ser, "FORWARD  vx=+0.10", 0.10, 0.00, 0.00)
s_back, p_back = run_motion_test(ser, "BACKWARD vx=-0.10", -0.10, 0.00, 0.00)
s_rotL, p_rotL = run_motion_test(ser, "ROTATE_L vz=+0.40", 0.00, 0.00, 0.40)
s_rotR, p_rotR = run_motion_test(ser, "ROTATE_R vz=-0.40", 0.00, 0.00, -0.40)
s_strL, p_strL = run_motion_test(ser, "STRAFE_L vy=+0.10", 0.00, 0.10, 0.00)

ser.close()

# ── Analysis ──────────────────────────────────────────────────────────────────
print(f"\n{'=' * 60}")
print(" ANALYSIS — which bytes respond to which motion")
print(f"{'=' * 60}")
print(
    f"  {'Field':6s}  {'Rest':>8s}  {'Fwd':>8s}  {'Back':>8s}  {'RotL':>8s}  {'StrafeL':>8s}  Notes"
)
print("  " + "-" * 75)

for field, offset in FIELDS:
    r = s_rest.get(field, (0, 0))[0]
    f = s_fwd.get(field, (0, 0))[0]
    b = s_back.get(field, (0, 0))[0]
    rl = s_rotL.get(field, (0, 0))[0]
    sl = s_strL.get(field, (0, 0))[0]

    fwd_delta = abs(f - r)
    rot_delta = abs(rl - r)
    str_delta = abs(sl - r)

    notes = []
    if fwd_delta > 20 and rot_delta > 20 and str_delta < 20:
        notes.append("all-wheels (fwd+rot)")
    elif fwd_delta > 20 and rot_delta > 20:
        notes.append("wheel speed")
    elif rot_delta > 20 and str_delta > 20 and fwd_delta < 20:
        notes.append("rotation-sensitive")
    elif fwd_delta > 20:
        notes.append("responds-to-forward")
    elif rot_delta > 20:
        notes.append("responds-to-rotation")
    elif fwd_delta < 5 and rot_delta < 5 and str_delta < 5:
        notes.append("static/unrelated")

    # Check if forward and backward are opposite signs
    if abs(f) > 20 and abs(b) > 20 and f * b < 0:
        notes.append("sign-flips-on-reverse ✓")

    print(
        f"  {field:6s}  {r:8.1f}  {f:8.1f}  {b:8.1f}  {rl:8.1f}  {sl:8.1f}  {', '.join(notes)}"
    )

print()
print("Fields where forward+backward flip sign AND magnitude is consistent")
print("are the wheel encoder/speed fields. Look for 4 such fields.")
print()
print("Expected mecanum pattern for forward motion (+vx):")
print("  All 4 wheels positive (or all negative depending on sign convention)")
print("Expected pattern for rotate-left (+vz):")
print("  FL(-), FR(+), BL(-), BR(+)  OR  FL(+), FR(-), BL(+), BR(-)")
print()
