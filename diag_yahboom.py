#!/usr/bin/env python3
"""
Yahboom hardware diagnostic — run WITHOUT ROS.

Usage:
    python3 diag_yahboom.py                       # uses /dev/ttyUSB0
    python3 diag_yahboom.py /dev/ttyUSB1

What it does:
    1. Check serial port is accessible
    2. Send SET_CAR_TYPE init (mecanum X3)
    3. BEEP — listen for buzzer (audio confirmation board is alive)
    4. Read back RX packets for 2 s (check IMU / velocity feedback)
    5. Send a small forward motion command for 1 s, then stop
    6. Print pass/fail summary
"""

import sys
import time
import struct
import serial

PORT = sys.argv[1] if len(sys.argv) > 1 else "/dev/ttyUSB0"
BAUD = 115200
TIMEOUT = 1.0

# ── protocol constants ──────────────────────────────────────────────────────
HEAD_TX = 0xFF
DEVICE_ID = 0xFC
HEAD_RX = 0xFB
_COMPL = 257 - DEVICE_ID  # = 5


def _checksum(packet):
    return (sum(packet) + _COMPL) & 0xFF


def build_packet(func_id, payload: bytes) -> bytes:
    pkt = [HEAD_TX, DEVICE_ID, 0, func_id] + list(payload)
    pkt[2] = len(pkt) - 1
    pkt.append(_checksum(pkt))
    return bytes(pkt)


def pkt_set_car_type(car_type=1):
    return build_packet(0x15, struct.pack("<b", car_type))


def pkt_beep(ms):
    return build_packet(0x02, struct.pack("<h", ms))


def pkt_motion(vx, vy, vz, car_type=1):
    return build_packet(
        0x12,
        struct.pack("<bhhh", car_type, int(vx * 1000), int(vy * 1000), int(vz * 1000)),
    )


def pkt_stop():
    return pkt_motion(0, 0, 0)


# ── helpers ─────────────────────────────────────────────────────────────────
def send(ser, pkt, label=""):
    hex_str = " ".join(f"{b:02X}" for b in pkt)
    print(f"  TX [{label}]: {hex_str}")
    ser.write(pkt)
    time.sleep(0.01)


def read_packets(ser, duration=2.0):
    """Read RX data for `duration` seconds, parse and print each packet."""
    buf = b""
    deadline = time.time() + duration
    seen = {"vel": False, "accel": False, "gyro": False, "att": False}

    while time.time() < deadline:
        chunk = ser.read(ser.in_waiting or 1)
        buf += chunk

        # scan for 0xFF 0xFB headers
        i = 0
        while i < len(buf) - 3:
            if buf[i] == HEAD_TX and buf[i + 1] == HEAD_RX:
                length = buf[i + 2]
                total = 2 + length
                if i + total > len(buf):
                    break
                pkt = buf[i : i + total]
                pkt_type = pkt[3]
                payload = pkt[4:-1]

                if pkt_type == 0x0C and len(payload) >= 6:
                    vx, vy, vz = struct.unpack_from("<hhh", payload)
                    print(
                        f"  RX VEL  vx={vx / 1000:.3f} vy={vy / 1000:.3f} vz={vz / 1000:.3f} m/s"
                    )
                    seen["vel"] = True
                elif pkt_type == 0x61 and len(payload) >= 6:
                    ax, ay, az = struct.unpack_from("<hhh", payload)
                    print(
                        f"  RX ACCEL ax={ax / 100:.2f} ay={ay / 100:.2f} az={az / 100:.2f} (raw×0.01)"
                    )
                    seen["accel"] = True
                elif pkt_type == 0x62 and len(payload) >= 6:
                    gx, gy, gz = struct.unpack_from("<hhh", payload)
                    print(
                        f"  RX GYRO  gx={gx / 1000:.3f} gy={gy / 1000:.3f} gz={gz / 1000:.3f} rad/s"
                    )
                    seen["gyro"] = True
                elif pkt_type == 0x63 and len(payload) >= 6:
                    r, p, y = struct.unpack_from("<hhh", payload)
                    print(
                        f"  RX ATT   roll={r / 100:.1f}° pitch={p / 100:.1f}° yaw={y / 100:.1f}°"
                    )
                    seen["att"] = True
                else:
                    hex_payload = " ".join(f"{b:02X}" for b in payload)
                    print(f"  RX UNKNOWN type=0x{pkt_type:02X} payload=[{hex_payload}]")
                i += total
            else:
                i += 1
        buf = buf[i:]

    return seen


# ── main ────────────────────────────────────────────────────────────────────
def main():
    results = {}

    # ── 1. Open serial port ──────────────────────────────────────────────────
    print(f"\n{'=' * 60}")
    print(f" Yahboom Hardware Diagnostic  —  {PORT} @ {BAUD}")
    print(f"{'=' * 60}\n")

    print(f"[1] Opening {PORT} ...")
    try:
        ser = serial.Serial(port=PORT, baudrate=BAUD, timeout=TIMEOUT)
        print("    OK — port open")
        results["serial_open"] = True
    except serial.SerialException as e:
        print(f"    FAIL: {e}")
        print("\n  Possible causes:")
        print("    • Board not plugged in / USB cable fault")
        print("    • Wrong port — try: ls /dev/ttyUSB*")
        print(
            "    • Permissions — run: sudo usermod -aG dialout $USER  (then re-login)"
        )
        return

    # ── 2. SET_CAR_TYPE ──────────────────────────────────────────────────────
    print("\n[2] Sending SET_CAR_TYPE=1 (Mecanum X3) × 5 ...")
    for _ in range(5):
        send(ser, pkt_set_car_type(1), "SET_CAR_TYPE")
        time.sleep(0.05)
    results["car_type_sent"] = True
    print("    Sent — waiting 300 ms for board init ...")
    time.sleep(0.3)

    # ── 3. BEEP ──────────────────────────────────────────────────────────────
    print("\n[3] Sending BEEP (300 ms) — listen for buzzer ...")
    send(ser, pkt_beep(300), "BEEP")
    time.sleep(0.5)
    print("    Did you hear a beep? (Y to mark PASS, anything else = FAIL)")
    try:
        ans = input("    > ").strip().lower()
        results["beep"] = ans == "y"
    except EOFError:
        results["beep"] = None
        print("    (non-interactive mode — skipping beep check)")

    # ── 4. Read RX packets ───────────────────────────────────────────────────
    print("\n[4] Reading RX packets for 3 s (board should stream IMU + velocity) ...")
    seen = read_packets(ser, duration=3.0)
    results["rx_velocity"] = seen["vel"]
    results["rx_imu"] = seen["accel"] or seen["gyro"] or seen["att"]

    if not any(seen.values()):
        print(
            "    WARNING: no valid RX packets received — board may be off or wrong port"
        )
    else:
        print(
            f"    Received: vel={seen['vel']} accel={seen['accel']} gyro={seen['gyro']} att={seen['att']}"
        )

    # ── 5. Motion test ───────────────────────────────────────────────────────
    print("\n[5] Motion test — sending vx=0.10 m/s forward for 1 s ...")
    print("    Place robot on floor with space ahead. Press Enter when ready.")
    try:
        input("    > ")
    except EOFError:
        pass

    t_end = time.time() + 1.0
    while time.time() < t_end:
        send(ser, pkt_motion(0.10, 0.0, 0.0), "MOTION fwd")
        time.sleep(0.05)

    send(ser, pkt_stop(), "STOP")
    print("    Did the robot move forward? (Y/N)")
    try:
        ans = input("    > ").strip().lower()
        results["motion"] = ans == "y"
    except EOFError:
        results["motion"] = None
        print("    (non-interactive — skipping motion check)")

    ser.close()

    # ── Summary ──────────────────────────────────────────────────────────────
    print(f"\n{'=' * 60}")
    print(" SUMMARY")
    print(f"{'=' * 60}")
    checks = [
        ("serial_open", "Serial port opens"),
        ("car_type_sent", "SET_CAR_TYPE sent"),
        ("beep", "Buzzer heard"),
        ("rx_velocity", "Velocity RX packets received"),
        ("rx_imu", "IMU RX packets received"),
        ("motion", "Robot moves on motion command"),
    ]
    for key, label in checks:
        val = results.get(key)
        if val is True:
            status = "PASS"
        elif val is False:
            status = "FAIL"
        else:
            status = "SKIP"
        print(f"  {status:<6} {label}")

    print()
    if not results.get("serial_open"):
        print("→ Fix serial port first (check USB cable, port, permissions).")
    elif not results.get("beep"):
        print(
            "→ Board reachable but no beep: check board power (12V supply), fuse, or firmware."
        )
    elif not results.get("rx_velocity") and not results.get("rx_imu"):
        print("→ TX works but no RX: board may need SET_CAR_TYPE to start streaming.")
        print("  Try power-cycling the board and running this script again.")
    elif not results.get("motion"):
        print(
            "→ Serial and IMU OK but no motion: check motor connectors, E-stop switch,"
        )
        print("  and that CAR_TYPE=1 (Mecanum X3) is accepted by the firmware version.")
    else:
        print("→ All checks passed — hardware appears healthy.")
    print()


if __name__ == "__main__":
    main()
