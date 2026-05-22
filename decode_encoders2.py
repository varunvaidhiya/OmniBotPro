#!/usr/bin/env python3
"""
Yahboom 0x0E encoder delta decoder — v2.

Tracks CONSECUTIVE deltas between packets (not means vs a baseline),
and tests at higher speed so signal is above noise.

Usage:
    python3 decode_encoders2.py              # /dev/ttyUSB0
    python3 decode_encoders2.py /dev/ttyUSB1

Output: for each motion type, shows mean delta-per-packet for every int16
field. Wheel encoder fields will have:
  - Consistent sign (positive or negative) during motion
  - Sign that flips when direction reverses
  - Near-zero delta when stopped
"""

import sys
import time
import struct
import serial
import statistics

PORT = sys.argv[1] if len(sys.argv) > 1 else "/dev/ttyUSB0"
BAUD = 115200

HEAD_TX, DEVICE_ID, HEAD_RX = 0xFF, 0xFC, 0xFB


def _ck(pkt):
    return (sum(pkt) + (257 - DEVICE_ID)) & 0xFF


def build(fn, payload):
    p = [HEAD_TX, DEVICE_ID, 0, fn] + list(payload)
    p[2] = len(p) - 1
    p.append(_ck(p))
    return bytes(p)


PKT_STOP = build(0x12, struct.pack("<bhhh", 1, 0, 0, 0))
PKT_CAR_TYPE = build(0x15, struct.pack("<b", 1))


def motion(vx, vy, vz):
    return build(
        0x12, struct.pack("<bhhh", 1, int(vx * 1000), int(vy * 1000), int(vz * 1000))
    )


FIELDS = [
    (f"b{i * 2:02d}", i * 2) for i in range(9)
]  # 9 int16 fields in 18-byte payload


# ── read & parse raw packets ────────────────────────────────────────────────
def read_raw(ser, duration):
    """Return list of (type, payload_bytes) for all complete RX packets."""
    buf, pkts, deadline = b"", [], time.time() + duration
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
                pkts.append((pkt[3], bytes(pkt[4:-1])))
                i += tot
            else:
                i += 1
        buf = buf[i:]
    return pkts


def delta_stats(pkt0e_list):
    """Compute per-field mean delta between consecutive 0x0E packets."""
    if len(pkt0e_list) < 2:
        return {}
    result = {}
    for label, offset in FIELDS:
        deltas = []
        for a, b in zip(pkt0e_list, pkt0e_list[1:]):
            if len(a) >= offset + 2 and len(b) >= offset + 2:
                va = struct.unpack_from("<h", a, offset)[0]
                vb = struct.unpack_from("<h", b, offset)[0]
                d = vb - va
                # Handle int16 wraparound (e.g., 32760 → -32760)
                if d > 32767:
                    d -= 65536
                if d < -32768:
                    d += 65536
                deltas.append(d)
        if deltas:
            result[label] = (
                statistics.mean(deltas),
                statistics.stdev(deltas) if len(deltas) > 1 else 0,
                len(deltas),
            )
    return result


def vel_stats(pkt_list):
    """Mean velocity feedback (0x0C) in m/s."""
    vx_list = []
    for ptype, payload in pkt_list:
        if ptype == 0x0C and len(payload) >= 6:
            vx, vy, vz = struct.unpack_from("<hhh", payload)
            vx_list.append(vx / 1000.0)
    return statistics.mean(vx_list) if vx_list else None


def run_test(ser, label, vx, vy, vz, speed_s=2.5, settle_s=2.0):
    ser.reset_input_buffer()
    pkt0e, pkts_all = [], []
    deadline = time.time() + speed_s
    while time.time() < deadline:
        ser.write(motion(vx, vy, vz))
        time.sleep(0.004)
        raw = read_raw(ser, 0.045)
        for ptype, payload in raw:
            if ptype == 0x0E and len(payload) >= 18:
                pkt0e.append(payload)
        pkts_all += raw

    for _ in range(5):
        ser.write(PKT_STOP)
        time.sleep(0.02)
    time.sleep(settle_s)

    ds = delta_stats(pkt0e)
    vfb = vel_stats(pkts_all)
    print(
        f"\n  ── {label}  (0x0E pkts={len(pkt0e)})  vel_feedback_vx≈{vfb:.3f} m/s ──"
        if vfb is not None
        else f"\n  ── {label}  (0x0E pkts={len(pkt0e)}) ──"
    )
    print(f"  {'Field':6s}  {'mean_delta':>11s}  {'stdev':>8s}  note")
    for label2, offset in FIELDS:
        if label2 not in ds:
            continue
        m, s, n = ds[label2]
        flag = ""
        if abs(m) > 3 * s and abs(m) > 1:
            flag = " ← SIGNAL"
        elif abs(m) < 1 and s < 3:
            flag = " (noise)"
        print(f"  {label2:6s}  {m:>11.2f}  {s:>8.2f}{flag}")
    return ds


print(f"\n{'=' * 65}")
print(f" Yahboom Encoder Delta Decoder v2  —  {PORT}")
print("  Tests run at higher speed so encoder signal > noise.")
print("  delta = value[n+1] - value[n]  between consecutive 0x0E pkts")
print(f"{'=' * 65}")

ser = serial.Serial(port=PORT, baudrate=BAUD, timeout=0.05)

# ── Init ─────────────────────────────────────────────────────────────────────
print("\n[Init] Stopping + setting CAR_TYPE ...")
for _ in range(5):
    ser.write(PKT_STOP)
    time.sleep(0.02)
time.sleep(0.3)
for _ in range(5):
    ser.write(PKT_CAR_TYPE)
    time.sleep(0.05)
time.sleep(0.3)
for _ in range(3):
    ser.write(PKT_STOP)
    time.sleep(0.02)
print("       Waiting 3 s for wheels to fully stop ...")
time.sleep(3.0)
ser.reset_input_buffer()

# ── Baseline (stationary) ─────────────────────────────────────────────────────
print("\n[1] Baseline — stationary 2 s ...")
pkts_rest = read_raw(ser, 2.0)
pkt0e_rest = [p for t, p in pkts_rest if t == 0x0E and len(p) >= 18]
ds_rest = delta_stats(pkt0e_rest)
vfb_rest = vel_stats(pkts_rest)
print(
    f"  0x0C vel_feedback at rest: vx≈{vfb_rest:.3f} m/s"
    if vfb_rest
    else "  no 0x0C packets"
)
print(f"  {'Field':6s}  {'mean_delta':>11s}  {'stdev':>8s}")
for lbl, off in FIELDS:
    if lbl not in ds_rest:
        continue
    m, s, n = ds_rest[lbl]
    print(f"  {lbl:6s}  {m:>11.2f}  {s:>8.2f}")

# ── Motion tests (higher speed) ───────────────────────────────────────────────
print("\n[2] Motion tests at higher speed (place robot on clear floor) ...")
input("    Press Enter when ready ...")

ds_fwd = run_test(ser, "FORWARD  vx=+0.20", 0.20, 0.00, 0.00)
ds_back = run_test(ser, "BACKWARD vx=-0.20", -0.20, 0.00, 0.00)
ds_rotL = run_test(ser, "ROTATE_L vz=+0.60", 0.00, 0.00, 0.60)
ds_rotR = run_test(ser, "ROTATE_R vz=-0.60", 0.00, 0.00, -0.60)
ds_strL = run_test(ser, "STRAFE_L vy=+0.20", 0.00, 0.20, 0.00)
ds_strR = run_test(ser, "STRAFE_R vy=-0.20", 0.00, -0.20, 0.00)

ser.close()

# ── Summary ───────────────────────────────────────────────────────────────────
print(f"\n{'=' * 65}")
print(" DELTA SUMMARY — mean delta per 0x0E packet during each motion")
print(f"{'=' * 65}")
print(
    f"  {'Field':6s}  {'Rest':>7s}  {'Fwd':>7s}  {'Back':>7s}  {'RotL':>7s}  {'RotR':>7s}  {'StrL':>7s}  {'StrR':>7s}"
)
print("  " + "-" * 72)

wheel_candidates = []
for lbl, off in FIELDS:
    r = ds_rest.get(lbl, (0, 0, 0))[0]
    f = ds_fwd.get(lbl, (0, 0, 0))[0]
    b = ds_back.get(lbl, (0, 0, 0))[0]
    rl = ds_rotL.get(lbl, (0, 0, 0))[0]
    rr = ds_rotR.get(lbl, (0, 0, 0))[0]
    sl = ds_strL.get(lbl, (0, 0, 0))[0]
    sr = ds_strR.get(lbl, (0, 0, 0))[0]

    signal_fwd = abs(f) > max(2, 5 * abs(r))
    flips_fb = f * b < 0
    flips_rot = rl * rr < 0
    flips_str = sl * sr < 0

    notes = []
    if signal_fwd and flips_fb:
        notes.append("WHEEL ✓ (fwd/back flip)")
    if flips_rot:
        notes.append("rot-sensitive")
    if flips_str:
        notes.append("strafe-sensitive")
    if not signal_fwd and abs(r) < 2:
        notes.append("noise/unrelated")

    if "WHEEL ✓" in " ".join(notes):
        wheel_candidates.append(lbl)

    print(
        f"  {lbl:6s}  {r:>7.1f}  {f:>7.1f}  {b:>7.1f}  {rl:>7.1f}  {rr:>7.1f}  {sl:>7.1f}  {sr:>7.1f}  {'  '.join(notes)}"
    )

print()
if wheel_candidates:
    print(f"  Wheel encoder candidate fields: {wheel_candidates}")
    print()
    print("  Expected mecanum pattern (forward motion, standard X3 wiring):")
    print("    FL and BL: same sign")
    print("    FR and BR: opposite sign to FL/BL")
    print("  For rotation: FL and BL opposite to FR and BR")
else:
    print("  No clear wheel encoder fields detected.")
    print("  Try running at even higher speed, or the 0x0E format is not raw encoder.")
print()
