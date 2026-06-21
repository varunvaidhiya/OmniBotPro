/*
 * Yahboom (Rosmaster) serial protocol — browser encoder.
 *
 * A TypeScript mirror of packages/yahboom_ros2/protocol.py so the USB (Web
 * Serial) and Bluetooth (BLE) transports can drive an OmniBot-class robot
 * directly, with no ROS in the loop.
 *
 *   TX packet: [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
 *     LEN      = 1 (device id) + 1 (len) + 1 (func) + N (payload)  = N + 3
 *     CHECKSUM = (sum(all bytes before checksum) + 5) & 0xFF
 *                (the +5 = 257 − 0xFC cancels the 0xFF/0xFC header pair, so the
 *                 checksum reduces to (LEN + FUNC + Σpayload) & 0xFF)
 *
 * Keep this in lock-step with packages/yahboom_ros2/protocol.py and
 * confirmed_protocol.py — they are the sources of truth.
 */

export const HEAD_TX = 0xff;
export const DEVICE_ID = 0xfc;
export const HEAD_RX = 0xfb;

export const FUNC_BEEP = 0x02;
export const FUNC_MOTOR = 0x10;
export const FUNC_MOTION = 0x12;
export const FUNC_SET_CAR_TYPE = 0x15;

export const CAR_TYPE_MECANUM_X3 = 1;

// RX type codes (byte index 3 of an incoming 0xFB packet).
export const TYPE_VELOCITY = 0x0c;
export const TYPE_ACCEL = 0x61;
export const TYPE_GYRO = 0x62;
export const TYPE_ATTITUDE = 0x63;

function clampInt16(v: number): number {
  return Math.max(-32768, Math.min(32767, Math.round(v)));
}

function int16le(v: number): [number, number] {
  const x = clampInt16(v) & 0xffff;
  return [x & 0xff, (x >> 8) & 0xff];
}

/** Frame a function + payload into a complete checksummed TX packet. */
export function buildPacket(func: number, payload: number[]): Uint8Array {
  const len = payload.length + 3; // device id + len + func + payload
  const before = [HEAD_TX, DEVICE_ID, len, func, ...payload];
  let sum = 0;
  for (const b of before) sum += b;
  const checksum = (sum + 5) & 0xff;
  return new Uint8Array([...before, checksum]);
}

/**
 * Motion command: body-frame velocities in m/s and rad/s.
 * Payload = <b h h h> = car_type, vx*1000, vy*1000, omega*1000 (little-endian).
 */
export function packetMotion(
  vx: number,
  vy: number,
  omega: number,
  carType: number = CAR_TYPE_MECANUM_X3,
): Uint8Array {
  const payload = [
    carType & 0xff,
    ...int16le(vx * 1000),
    ...int16le(vy * 1000),
    ...int16le(omega * 1000),
  ];
  return buildPacket(FUNC_MOTION, payload);
}

/** Set the car type — send once on connect (X3 mecanum = 1). */
export function packetSetCarType(carType: number = CAR_TYPE_MECANUM_X3): Uint8Array {
  return buildPacket(FUNC_SET_CAR_TYPE, [carType & 0xff]);
}

/** Buzzer on for `ms` milliseconds. */
export function packetBeep(ms: number): Uint8Array {
  return buildPacket(FUNC_BEEP, int16le(ms));
}

/** Direct four-wheel velocities (int16 each). */
export function packetMotor(fl: number, fr: number, rl: number, rr: number): Uint8Array {
  return buildPacket(FUNC_MOTOR, [
    ...int16le(fl),
    ...int16le(fr),
    ...int16le(rl),
    ...int16le(rr),
  ]);
}

/** Validate that a received buffer is a well-formed 0xFB Yahboom RX frame. */
export function isValidRxFrame(buf: Uint8Array): boolean {
  if (buf.length < 4 || buf[0] !== HEAD_RX) return false;
  const len = buf[1];
  return buf.length >= len + 2;
}
