/*
 * Teleop state machine and simulation for OhhO Pilot.
 *
 * Models the real OmniBot teleop pipeline — velocity clamping, ramp limiting,
 * control-mode mux, e-stop — but runs entirely in-browser with deterministic
 * simulated values. No WebSocket connection is opened.
 */

// ── Types ─────────────────────────────────────────────────────────────────────

export type ControlMode = "teleop" | "nav2" | "vla";

export interface Velocity {
  /** Forward/backward, m/s. */
  linearX: number;
  /** Left/right strafe (mecanum only), m/s. */
  linearY: number;
  /** Yaw rotation, rad/s. */
  angularZ: number;
}

export interface TeleopState {
  connected: boolean;
  connecting: boolean;
  controlMode: ControlMode;
  vel: Velocity;
  /** Arm joint positions in radians, indexed by joint order. */
  armPositions: number[];
  eStop: boolean;
  /** Simulated round-trip latency, ms. */
  latency: number;
  /** Simulated uptime since connection, seconds. */
  uptime: number;
  /** Messages per second (simulated). */
  msgRate: number;
  /** Recent latency readings for the sparkline (last 20 ticks). */
  latencyHistory: number[];
}

// ── Constants (mirror the real robot's safety limits) ──────────────────────────

/** Maximum forward/strafe velocity, m/s. */
const MAX_LIN_VEL = 0.2;
/** Maximum angular velocity, rad/s. */
const MAX_ANG_VEL = 1.0;
/** Ramp step per tick (matches Yahboom driver's 0.05 m/s per 50 ms). */
const RAMP_STEP_LIN = 0.05;
const RAMP_STEP_ANG = 0.15;

// ── Helpers ───────────────────────────────────────────────────────────────────

function clamp(v: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, v));
}

/** Ramp a value toward a target at most `step` per tick. */
function rampToward(current: number, target: number, step: number): number {
  const diff = target - current;
  if (Math.abs(diff) <= step) return target;
  return current + Math.sign(diff) * step;
}

/** Generate simulated latency with realistic jitter. */
function jitteredLatency(base: number): number {
  return Math.round(base + (Math.random() - 0.5) * 16);
}

// ── State factory ─────────────────────────────────────────────────────────────

export function initialTeleopState(armJointCount: number): TeleopState {
  return {
    connected: false,
    connecting: false,
    controlMode: "teleop",
    vel: { linearX: 0, linearY: 0, angularZ: 0 },
    armPositions: new Array(armJointCount).fill(0),
    eStop: false,
    latency: 0,
    uptime: 0,
    msgRate: 0,
    latencyHistory: new Array(20).fill(0),
  };
}

// ── Velocity pipeline ─────────────────────────────────────────────────────────

/**
 * Process raw joystick input through the safety pipeline.
 * Returns the new velocity after clamping and ramping.
 */
export function applyJoystick(
  current: Velocity,
  joystickX: number,
  joystickY: number,
  maxLin: number,
  maxAng: number,
  eStop: boolean,
): Velocity {
  if (eStop) return { linearX: 0, linearY: 0, angularZ: 0 };

  const effectiveMaxLin = Math.min(maxLin, MAX_LIN_VEL);
  const effectiveMaxAng = Math.min(maxAng, MAX_ANG_VEL);

  // Joystick Y → forward/backward, X → angular (or strafe for mecanum)
  const targetLinX = clamp(-joystickY * effectiveMaxLin, -effectiveMaxLin, effectiveMaxLin);
  const targetAngZ = clamp(-joystickX * effectiveMaxAng, -effectiveMaxAng, effectiveMaxAng);

  return {
    linearX: rampToward(current.linearX, targetLinX, RAMP_STEP_LIN),
    linearY: 0, // strafe handled separately for mecanum
    angularZ: rampToward(current.angularZ, targetAngZ, RAMP_STEP_ANG),
  };
}

/**
 * Apply strafe input (mecanum only). Separate from the main joystick to
 * support a dedicated strafe control.
 */
export function applyStrafe(
  current: Velocity,
  strafeInput: number,
  maxLin: number,
  eStop: boolean,
): Velocity {
  if (eStop) return { ...current, linearY: 0 };
  const effectiveMax = Math.min(maxLin, MAX_LIN_VEL);
  const target = clamp(strafeInput * effectiveMax, -effectiveMax, effectiveMax);
  return {
    ...current,
    linearY: rampToward(current.linearY, target, RAMP_STEP_LIN),
  };
}

/** Zero out all velocities (called on e-stop or disconnect). */
export function zeroVelocity(): Velocity {
  return { linearX: 0, linearY: 0, angularZ: 0 };
}

// ── Connection simulation ─────────────────────────────────────────────────────

export interface ConnectionSim {
  tick: (state: TeleopState) => TeleopState;
}

/**
 * Create a simulated connection that produces jittered latency readings,
 * uptime, and message rate — mimicking a real ROSBridge WebSocket.
 */
export function createConnectionSim(): ConnectionSim {
  const baseLatency = 24; // ms base RTT
  let tickCount = 0;

  return {
    tick(state: TeleopState): TeleopState {
      if (!state.connected) {
        return {
          ...state,
          latency: 0,
          uptime: 0,
          msgRate: 0,
          latencyHistory: state.latencyHistory,
        };
      }
      tickCount++;
      const lat = jitteredLatency(baseLatency);
      const history = [...state.latencyHistory.slice(1), lat];
      return {
        ...state,
        latency: lat,
        uptime: tickCount,
        msgRate: Math.round(18 + Math.random() * 8), // 18-26 msg/s
        latencyHistory: history,
      };
    },
  };
}

// ── Control mode labels ───────────────────────────────────────────────────────

export const CONTROL_MODES: { id: ControlMode; label: string; desc: string }[] = [
  { id: "teleop", label: "Teleop", desc: "Manual joystick control" },
  { id: "nav2", label: "Nav2", desc: "Autonomous navigation" },
  { id: "vla", label: "VLA", desc: "AI vision-language-action" },
];
