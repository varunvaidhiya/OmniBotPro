/*
 * Robot profile catalog for OhhO Pilot.
 *
 * Pre-loaded robot profiles + a custom builder type. Each profile describes the
 * kinematic footprint the teleop console needs to know about: base type, arm
 * presence, joint count, velocity limits, and camera topology.
 *
 * This module is pure data — no hooks, no React, importable from server or
 * client components.
 */

// ── Types ─────────────────────────────────────────────────────────────────────

export type BaseType = "mecanum" | "diff" | "ackermann";

export interface RobotProfile {
  id: string;
  name: string;
  desc: string;
  baseType: BaseType;
  hasArm: boolean;
  /** Number of arm joints (0 when !hasArm). */
  armJoints: number;
  /** Max linear velocity, m/s. */
  maxLinVel: number;
  /** Max angular velocity, rad/s. */
  maxAngVel: number;
  /** Camera topic labels available on this robot. */
  cameras: string[];
  /** SVG icon path data (viewBox 0 0 24 24). */
  iconPath: string;
}

export interface ArmJoint {
  name: string;
  /** Short label for the slider UI. */
  label: string;
  min: number;
  max: number;
  /** Current simulated position, radians. */
  home: number;
}

// ── Default arm joints (SO-101 6-DOF) ─────────────────────────────────────────

export const DEFAULT_ARM_JOINTS: ArmJoint[] = [
  { name: "arm_shoulder_pan", label: "Pan", min: -3.14, max: 3.14, home: 0 },
  { name: "arm_shoulder_lift", label: "Lift", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_elbow_flex", label: "Elbow", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_wrist_flex", label: "Wrist", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_wrist_roll", label: "Roll", min: -3.14, max: 3.14, home: 0 },
  { name: "arm_gripper", label: "Grip", min: -0.1, max: 0.8, home: 0 },
];

// ── Pre-loaded profiles ───────────────────────────────────────────────────────

export const PROFILES: RobotProfile[] = [
  {
    id: "omnibot",
    name: "OmniBot",
    desc: "Mecanum-wheel mobile manipulator with SO-101 6-DOF arm, depth + wrist cameras.",
    baseType: "mecanum",
    hasArm: true,
    armJoints: 6,
    maxLinVel: 0.2,
    maxAngVel: 1.0,
    cameras: ["front", "wrist", "BEV"],
    iconPath: "M3 6h18v12H3zM7 18v2M17 18v2M12 6V4M6 10h2M16 10h2M10 14h4",
  },
  {
    id: "diff-drive",
    name: "Diff-Drive Scout",
    desc: "Simple two-wheeled differential-drive base. No arm. Single front camera.",
    baseType: "diff",
    hasArm: false,
    armJoints: 0,
    maxLinVel: 0.5,
    maxAngVel: 2.0,
    cameras: ["front"],
    iconPath: "M5 8h14v8H5zM3 12h2M19 12h2M12 8V5",
  },
  {
    id: "ackermann",
    name: "Ackermann Rover",
    desc: "Car-like steering for outdoor delivery. No arm. Front + rear cameras.",
    baseType: "ackermann",
    hasArm: false,
    armJoints: 0,
    maxLinVel: 1.2,
    maxAngVel: 0.8,
    cameras: ["front", "rear"],
    iconPath: "M4 10h16v6H4zM6 16v2M18 16v2M8 10V7l4-2 4 2v3M12 10v6",
  },
];

/** Retrieve a profile by its id (returns the first if not found). */
export function getProfile(id: string): RobotProfile {
  return PROFILES.find((p) => p.id === id) ?? PROFILES[0];
}

/** Arm joints for a given profile (empty array for armless robots). */
export function jointsForProfile(profile: RobotProfile): ArmJoint[] {
  if (!profile.hasArm) return [];
  return DEFAULT_ARM_JOINTS.slice(0, profile.armJoints);
}

// ── Bridge: garage RobotConfig → Pilot RobotProfile ──────────────────────────
// The garage selection is the source of truth; this adapts any RobotConfig into
// the teleop profile Pilot's cockpit + simulators understand. Aerial / legged /
// fixed robots map to the closest controllable base for the sim.

import type { RobotConfig, DriveKind } from "@/lib/garage/robot-config";

function baseTypeFor(drive: DriveKind): BaseType {
  switch (drive) {
    case "mecanum":
      return "mecanum";
    case "ackermann":
      return "ackermann";
    case "differential":
    case "skid-steer":
    case "tracked":
    case "rocker-bogie":
      return "diff";
    default:
      // aerial / legged / thruster / fixed-base — drive holonomically in the sim
      return "mecanum";
  }
}

/** Build a Pilot teleop profile from the garage-selected robot. */
export function profileFromConfig(config: RobotConfig): RobotProfile {
  const cameras = config.sensors
    .filter((s) =>
      ["rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera", "bev"].includes(s.kind),
    )
    .map((s) => s.label.replace(/\s*camera\s*/i, "").trim().toLowerCase() || "cam");

  return {
    id: `garage:${config.robotId}`,
    name: config.name,
    desc: config.summary ?? `${config.driveLabel}${config.hasArm ? ` + ${config.armDof}-DOF arm` : ", no arm"}.`,
    baseType: baseTypeFor(config.drive),
    hasArm: config.hasArm,
    armJoints: config.armDof,
    maxLinVel: config.maxLinVel,
    maxAngVel: config.maxAngVel,
    cameras: cameras.length ? cameras : ["front"],
    iconPath: "M3 6h18v12H3zM7 18v2M17 18v2M12 6V4M6 10h2M16 10h2M10 14h4",
  };
}
