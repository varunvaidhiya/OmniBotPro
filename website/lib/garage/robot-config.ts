/*
 * RobotConfig — the single capability profile every OhhO console reads to
 * pre-configure itself for the robot the user picked in their garage.
 *
 * Until now the garage only stored loose display `specs` (a string map) per
 * HardwareModel, and every console hardcoded the OmniBot reference robot.
 * This module turns each catalog model into a STRUCTURED, deterministic config
 * (drive, DOF, joints, sensors, compute, state/action dims, ROS topics) so a
 * drone, a UR5e arm, a quadruped and OmniBot each get a correct, distinct
 * console — instantly and offline, with no AI call required.
 *
 * Three layers compose the final config (later wins):
 *   1. deriveRobotConfig()  — pure mapping from existing catalog fields.
 *   2. FLAGSHIP_OVERRIDES   — hand-tuned precision for marquee robots.
 *   3. AI enrichment        — optional, lazy, applied at runtime (lib/ai).
 *
 * Pure data + types only (no React, no client state) so it imports cleanly
 * from server components, client components and plain modules alike.
 */

import type { HardwareModel, RobotType, RobotCategoryId } from "./types";
import { getCategory } from "./types";
import {
  getHardwareModel,
  getRobotTypeForHardware,
} from "./robot-catalog";

// ── Drive / locomotion ───────────────────────────────────────────────────────

export type DriveKind =
  | "mecanum"
  | "differential"
  | "ackermann"
  | "skid-steer"
  | "tracked"
  | "rocker-bogie"
  | "gantry"
  | "quadrotor"
  | "hexacopter"
  | "vtol"
  | "fixed-wing"
  | "quadruped"
  | "hexapod"
  | "bipedal"
  | "thruster"
  | "fixed-base"
  | "wearable"
  | "unknown";

interface DriveSpec {
  label: string;
  /** Controllable base degrees of freedom (0 for a stationary arm). */
  baseDof: number;
  /** Action-vector labels for the base, e.g. ["vx","vy","ω"]. */
  actionLabels: string[];
  /** Can the base translate freely in-plane (no non-holonomic constraint)? */
  holonomic: boolean;
  /** Default top linear / angular speed when specs don't say. */
  maxLinVel: number;
  maxAngVel: number;
}

const DRIVE_SPECS: Record<DriveKind, DriveSpec> = {
  mecanum: { label: "Mecanum (holonomic)", baseDof: 3, actionLabels: ["vx", "vy", "ω"], holonomic: true, maxLinVel: 0.5, maxAngVel: 1.5 },
  differential: { label: "Differential drive", baseDof: 2, actionLabels: ["v", "ω"], holonomic: false, maxLinVel: 0.6, maxAngVel: 2.0 },
  ackermann: { label: "Ackermann steering", baseDof: 2, actionLabels: ["v", "δ"], holonomic: false, maxLinVel: 2.0, maxAngVel: 1.0 },
  "skid-steer": { label: "Skid-steer", baseDof: 2, actionLabels: ["v", "ω"], holonomic: false, maxLinVel: 1.5, maxAngVel: 1.5 },
  tracked: { label: "Tracked", baseDof: 2, actionLabels: ["v", "ω"], holonomic: false, maxLinVel: 1.5, maxAngVel: 1.0 },
  "rocker-bogie": { label: "Rocker-bogie (6-wheel)", baseDof: 2, actionLabels: ["v", "ω"], holonomic: false, maxLinVel: 0.3, maxAngVel: 0.5 },
  gantry: { label: "Cartesian gantry", baseDof: 3, actionLabels: ["x", "y", "z"], holonomic: true, maxLinVel: 0.2, maxAngVel: 0 },
  quadrotor: { label: "Quadrotor", baseDof: 4, actionLabels: ["vx", "vy", "vz", "ω"], holonomic: true, maxLinVel: 12, maxAngVel: 3.0 },
  hexacopter: { label: "Hexacopter", baseDof: 4, actionLabels: ["vx", "vy", "vz", "ω"], holonomic: true, maxLinVel: 10, maxAngVel: 2.5 },
  vtol: { label: "VTOL fixed-wing", baseDof: 4, actionLabels: ["vx", "vy", "vz", "ω"], holonomic: true, maxLinVel: 20, maxAngVel: 1.5 },
  "fixed-wing": { label: "Fixed-wing", baseDof: 4, actionLabels: ["thr", "roll", "pitch", "yaw"], holonomic: false, maxLinVel: 25, maxAngVel: 1.0 },
  quadruped: { label: "Quadruped gait", baseDof: 3, actionLabels: ["vx", "vy", "ω"], holonomic: true, maxLinVel: 3.0, maxAngVel: 2.0 },
  hexapod: { label: "Hexapod gait", baseDof: 3, actionLabels: ["vx", "vy", "ω"], holonomic: true, maxLinVel: 0.4, maxAngVel: 0.8 },
  bipedal: { label: "Bipedal gait", baseDof: 3, actionLabels: ["vx", "vy", "ω"], holonomic: true, maxLinVel: 1.4, maxAngVel: 1.0 },
  thruster: { label: "Thruster vectoring", baseDof: 4, actionLabels: ["surge", "sway", "heave", "yaw"], holonomic: true, maxLinVel: 1.5, maxAngVel: 1.0 },
  "fixed-base": { label: "Fixed base", baseDof: 0, actionLabels: [], holonomic: false, maxLinVel: 0, maxAngVel: 0 },
  wearable: { label: "Wearable", baseDof: 0, actionLabels: [], holonomic: false, maxLinVel: 0, maxAngVel: 0 },
  unknown: { label: "Mobile base", baseDof: 2, actionLabels: ["v", "ω"], holonomic: false, maxLinVel: 0.5, maxAngVel: 1.0 },
};

/** Map a free-text `locomotion` string (+ category fallback) to a DriveKind. */
export function parseDrive(locomotion: string, category: RobotCategoryId): DriveKind {
  const l = (locomotion || "").toLowerCase();
  if (l.includes("mecanum")) return "mecanum";
  if (l.includes("ackermann")) return "ackermann";
  if (l.includes("skid")) return "skid-steer";
  if (l.includes("rocker")) return "rocker-bogie";
  if (l.includes("gantry") || l.includes("cartesian")) return "gantry";
  if (l.includes("hexacopter")) return "hexacopter";
  if (l.includes("vtol")) return "vtol";
  if (l.includes("fixed-wing") || l.includes("fixed_wing")) return "fixed-wing";
  if (l.includes("quadrotor") || l.includes("quadcopter")) return "quadrotor";
  if (l.includes("quadruped")) return "quadruped";
  if (l.includes("hexapod")) return "hexapod";
  if (l.includes("biped")) return "bipedal";
  if (l.includes("thruster") || l.includes("marine") || l.includes("underwater")) return "thruster";
  if (l.includes("tracked") || l.includes("track")) return "tracked";
  if (l.includes("wearable")) return "wearable";
  if (l.includes("fixed-base") || l.includes("fixed_base")) return "fixed-base";
  if (l.includes("differential") || l.includes("diff")) return "differential";
  // category fallbacks for sparse/odd locomotion strings
  switch (category) {
    case "drones": return "quadrotor";
    case "industrial-arm": return "fixed-base";
    case "humanoid": return "bipedal";
    case "legged": return "quadruped";
    case "marine":
    case "underwater-rov": return "thruster";
    case "space": return "rocker-bogie";
    case "wheeled":
    case "delivery": return "differential";
    default: return "unknown";
  }
}

// ── Sensors, joints, compute ─────────────────────────────────────────────────

export type SensorKind =
  | "rgb_camera"
  | "depth_camera"
  | "stereo_camera"
  | "thermal_camera"
  | "fpv_camera"
  | "bev"
  | "lidar_2d"
  | "lidar_3d"
  | "imu"
  | "gps"
  | "sonar"
  | "barometer"
  | "encoder"
  | "force_torque";

export interface SensorSpec {
  kind: SensorKind;
  label: string;
  topic: string;
  hz: number;
}

export interface JointSpec {
  name: string;
  label: string;
  min: number;
  max: number;
  home: number;
}

export interface ComputeSpec {
  brain: string;
  accelerator?: string;
  vramGb?: number;
}

export interface RobotConfig {
  robotId: string;
  name: string;
  manufacturer: string;
  category: RobotCategoryId;
  drive: DriveKind;
  driveLabel: string;
  holonomic: boolean;
  baseDof: number;
  baseActionLabels: string[];
  hasArm: boolean;
  armDof: number;
  joints: JointSpec[];
  /** armDof + baseDof — what the consoles label as the robot's DOF. */
  totalDof: number;
  stateDim: number;
  actionDim: number;
  sensors: SensorSpec[];
  compute: ComputeSpec;
  payloadKg: number;
  maxLinVel: number;
  maxAngVel: number;
  ros: HardwareModel["ros"];
  rosTopics: {
    cmdVel: string;
    odom?: string;
    jointStates?: string;
    images: string[];
  };
  datasetName: string;
  capabilities: {
    hasArm: boolean;
    canNavigate: boolean;
    canManipulate: boolean;
    isAerial: boolean;
    isUnderwater: boolean;
    isStationary: boolean;
    isLegged: boolean;
  };
  /** Has this config been augmented by the Kimi enrichment route? */
  enriched: boolean;
  source: "derived" | "flagship" | "ai";
  /** Optional one-line summary the LLM can fill in. */
  summary?: string;
}

// SO-101 6-DOF arm — the OhhO reference arm (mirrors lib/pilot/robots.ts).
const SO101_JOINTS: JointSpec[] = [
  { name: "arm_shoulder_pan", label: "Pan", min: -3.14, max: 3.14, home: 0 },
  { name: "arm_shoulder_lift", label: "Lift", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_elbow_flex", label: "Elbow", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_wrist_flex", label: "Wrist", min: -1.57, max: 1.57, home: 0 },
  { name: "arm_wrist_roll", label: "Roll", min: -3.14, max: 3.14, home: 0 },
  { name: "arm_gripper", label: "Grip", min: -0.1, max: 0.8, home: 0 },
];

const GENERIC_JOINT_LABELS = ["Base", "Shoulder", "Elbow", "Wrist 1", "Wrist 2", "Wrist 3", "Wrist 4"];

/** Build `n` generic revolute joints (used when no flagship override exists). */
export function genericJoints(n: number): JointSpec[] {
  const out: JointSpec[] = [];
  for (let i = 0; i < n; i++) {
    out.push({
      name: `joint_${i + 1}`,
      label: GENERIC_JOINT_LABELS[i] ?? `J${i + 1}`,
      min: -3.14,
      max: 3.14,
      home: 0,
    });
  }
  return out;
}

/** Parse an integer DOF out of a loose spec value like "23–43" or "6 / 7". */
function parseDof(specs: Record<string, string>): number | undefined {
  const raw = specs.dof ?? specs.DOF ?? specs.joints;
  if (!raw) return undefined;
  const m = raw.match(/\d+/);
  return m ? parseInt(m[0], 10) : undefined;
}

/** Parse a speed like "0.2 m/s", "5 m/s", "6 km/h", "1.2 m/s" → m/s. */
function parseSpeed(specs: Record<string, string>): number | undefined {
  const raw = specs.speed ?? specs.topSpeed ?? specs.maxSpeed;
  if (!raw) return undefined;
  const m = raw.match(/([\d.]+)\s*(km\/h|kmh|m\/s)?/i);
  if (!m) return undefined;
  const val = parseFloat(m[1]);
  if (Number.isNaN(val)) return undefined;
  return /km/i.test(m[2] ?? "") ? val / 3.6 : val;
}

function parseCompute(specs: Record<string, string>, category: RobotCategoryId): ComputeSpec {
  const brain =
    specs.compute || specs.comp || specs.fc || specs.controller || "Onboard computer";
  const text = `${brain} ${specs.sensors ?? ""}`.toLowerCase();
  let accelerator: string | undefined;
  let vramGb: number | undefined;
  if (text.includes("jetson") || text.includes("orin") || text.includes("agx")) {
    accelerator = "NVIDIA Jetson";
  } else if (text.includes("hailo")) {
    accelerator = "Hailo NPU";
  } else if (text.includes("coral")) {
    accelerator = "Coral TPU";
  }
  // VLA-class robots (humanoids, mobile manipulators) imply a desktop GPU brain.
  if (!accelerator && (category === "humanoid" || category === "mobile-manipulator")) {
    accelerator = "NVIDIA GPU (desktop)";
    vramGb = 16;
  }
  return { brain, accelerator, vramGb };
}

/** Derive the sensor suite from category + drive (+ spec hints). */
function deriveSensors(
  category: RobotCategoryId,
  drive: DriveKind,
  hasArm: boolean,
  specs: Record<string, string>,
): SensorSpec[] {
  const s: SensorSpec[] = [];
  const specText = JSON.stringify(specs).toLowerCase();
  const imu: SensorSpec = { kind: "imu", label: "IMU", topic: "/imu/data", hz: 100 };

  switch (category) {
    case "mobile-manipulator": {
      s.push({ kind: "rgb_camera", label: "Front Camera", topic: "/camera/front/image_raw", hz: 30 });
      s.push({ kind: "rgb_camera", label: "Wrist Camera", topic: "/camera/wrist/image_raw", hz: 30 });
      s.push({ kind: "depth_camera", label: "Wrist Depth", topic: "/camera/wrist/depth/points", hz: 30 });
      s.push({ kind: "bev", label: "BEV Stitcher", topic: "/camera/base/bev/image_raw", hz: 10 });
      s.push({ kind: "lidar_2d", label: "2D Lidar", topic: "/scan", hz: 15 });
      s.push(imu);
      break;
    }
    case "wheeled":
    case "delivery": {
      s.push({ kind: "rgb_camera", label: "Front Camera", topic: "/camera/front/image_raw", hz: 30 });
      if (hasArm) s.push({ kind: "rgb_camera", label: "Wrist Camera", topic: "/camera/wrist/image_raw", hz: 30 });
      s.push({ kind: "depth_camera", label: "Depth Cloud", topic: "/camera/depth/points", hz: 30 });
      s.push({ kind: "lidar_2d", label: "2D Lidar", topic: "/scan", hz: 15 });
      s.push(imu);
      break;
    }
    case "drones": {
      s.push({ kind: "fpv_camera", label: "FPV Camera", topic: "/camera/fpv/image_raw", hz: 30 });
      s.push({ kind: "rgb_camera", label: "Down Camera", topic: "/camera/down/image_raw", hz: 30 });
      if (specText.includes("thermal")) s.push({ kind: "thermal_camera", label: "Thermal", topic: "/camera/thermal/image_raw", hz: 9 });
      s.push({ kind: "gps", label: "GPS", topic: "/gps/fix", hz: 10 });
      s.push({ kind: "barometer", label: "Barometer", topic: "/baro", hz: 50 });
      s.push({ ...imu, hz: 200 });
      break;
    }
    case "legged": {
      s.push({ kind: "rgb_camera", label: "Front Camera", topic: "/camera/front/image_raw", hz: 30 });
      s.push({ kind: "depth_camera", label: "Depth Cloud", topic: "/camera/depth/points", hz: 30 });
      s.push({ kind: "lidar_3d", label: "3D Lidar", topic: "/lidar/points", hz: 10 });
      s.push({ ...imu, hz: 200 });
      break;
    }
    case "humanoid": {
      s.push({ kind: "stereo_camera", label: "Head Stereo", topic: "/camera/head/image_raw", hz: 30 });
      s.push({ kind: "depth_camera", label: "Depth Cloud", topic: "/camera/depth/points", hz: 30 });
      s.push({ kind: "encoder", label: "Joint Encoders", topic: "/joint_states", hz: 200 });
      s.push({ ...imu, hz: 200 });
      break;
    }
    case "industrial-arm": {
      s.push({ kind: "rgb_camera", label: "Wrist Camera", topic: "/camera/wrist/image_raw", hz: 30 });
      if (specText.includes("torque")) s.push({ kind: "force_torque", label: "F/T Sensor", topic: "/wrench", hz: 500 });
      s.push({ kind: "encoder", label: "Joint Encoders", topic: "/joint_states", hz: 100 });
      break;
    }
    case "tracked": {
      s.push({ kind: "rgb_camera", label: "Front Camera", topic: "/camera/front/image_raw", hz: 30 });
      s.push({ kind: "lidar_3d", label: "3D Lidar", topic: "/lidar/points", hz: 10 });
      s.push({ kind: "gps", label: "GPS", topic: "/gps/fix", hz: 10 });
      s.push(imu);
      break;
    }
    case "marine": {
      s.push({ kind: "rgb_camera", label: "Deck Camera", topic: "/camera/deck/image_raw", hz: 15 });
      s.push({ kind: "gps", label: "GPS", topic: "/gps/fix", hz: 10 });
      s.push({ kind: "sonar", label: "Sonar", topic: "/sonar", hz: 5 });
      s.push(imu);
      break;
    }
    case "underwater-rov": {
      s.push({ kind: "rgb_camera", label: "Main Camera", topic: "/camera/main/image_raw", hz: 30 });
      s.push({ kind: "sonar", label: "Scanning Sonar", topic: "/sonar", hz: 5 });
      s.push({ kind: "barometer", label: "Depth (pressure)", topic: "/depth", hz: 20 });
      s.push(imu);
      break;
    }
    case "space": {
      s.push({ kind: "stereo_camera", label: "Nav Stereo", topic: "/camera/stereo/image_raw", hz: 10 });
      s.push({ ...imu, hz: 50 });
      break;
    }
    case "agricultural": {
      s.push({ kind: "rgb_camera", label: "Crop Camera", topic: "/camera/crop/image_raw", hz: 15 });
      s.push({ kind: "gps", label: "RTK GPS", topic: "/gps/fix", hz: 10 });
      s.push(imu);
      break;
    }
    case "swarm": {
      s.push({ kind: "rgb_camera", label: "Onboard Camera", topic: "/camera/image_raw", hz: 15 });
      s.push(imu);
      break;
    }
    case "inspection": {
      s.push({ kind: "rgb_camera", label: "PTZ Camera", topic: "/camera/ptz/image_raw", hz: 30 });
      if (specText.includes("thermal")) s.push({ kind: "thermal_camera", label: "Thermal", topic: "/camera/thermal/image_raw", hz: 9 });
      s.push(imu);
      break;
    }
    case "medical": {
      s.push({ kind: "stereo_camera", label: "Endoscope", topic: "/camera/endoscope/image_raw", hz: 30 });
      if (hasArm) s.push({ kind: "force_torque", label: "F/T Sensor", topic: "/wrench", hz: 500 });
      s.push({ kind: "encoder", label: "Joint Encoders", topic: "/joint_states", hz: 200 });
      break;
    }
    default: {
      s.push({ kind: "rgb_camera", label: "Camera", topic: "/camera/image_raw", hz: 30 });
      s.push(imu);
    }
  }
  return s;
}

// ── Derivation ───────────────────────────────────────────────────────────────

/** Build a deterministic RobotConfig from catalog data alone (no AI, no overrides). */
export function deriveRobotConfig(
  hw: HardwareModel,
  type: RobotType,
  categoryId: RobotCategoryId,
): RobotConfig {
  const drive = parseDrive(hw.locomotion, categoryId);
  const driveSpec = DRIVE_SPECS[drive];
  const baseDof = driveSpec.baseDof;

  const hasArm = hw.hasArm;
  const armDof = hasArm ? parseDof(hw.specs) ?? 6 : 0;
  const joints = hasArm ? genericJoints(armDof) : [];

  const sensors = deriveSensors(categoryId, drive, hasArm, hw.specs);
  const compute = parseCompute(hw.specs, categoryId);

  const speed = parseSpeed(hw.specs);
  const maxLinVel = speed ?? driveSpec.maxLinVel;
  const maxAngVel = driveSpec.maxAngVel;

  const totalDof = armDof + baseDof;
  const stateDim = totalDof;
  const actionDim = totalDof;

  const canNavigate = baseDof > 0;
  const canManipulate = hasArm;
  const isAerial = drive === "quadrotor" || drive === "hexacopter" || drive === "vtol" || drive === "fixed-wing";
  const isUnderwater = categoryId === "underwater-rov";
  const isStationary = baseDof === 0;
  const isLegged = drive === "quadruped" || drive === "hexapod" || drive === "bipedal";

  const images = sensors
    .filter((s) => ["rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera", "bev"].includes(s.kind))
    .map((s) => s.topic);

  const datasetName =
    hasArm && baseDof > 0
      ? "local/mobile_manipulation"
      : `local/${categoryId.replace(/-/g, "_")}`;

  return {
    robotId: hw.id,
    name: hw.name,
    manufacturer: hw.manufacturer,
    category: categoryId,
    drive,
    driveLabel: driveSpec.label,
    holonomic: driveSpec.holonomic,
    baseDof,
    baseActionLabels: driveSpec.actionLabels,
    hasArm,
    armDof,
    joints,
    totalDof,
    stateDim,
    actionDim,
    sensors,
    compute,
    payloadKg: hw.payloadKg,
    maxLinVel,
    maxAngVel,
    ros: hw.ros,
    rosTopics: {
      cmdVel: baseDof > 0 ? "/cmd_vel" : "",
      odom: baseDof > 0 ? "/odom" : undefined,
      jointStates: hasArm || isLegged || categoryId === "humanoid"
        ? (categoryId === "mobile-manipulator" || categoryId === "wheeled" ? "/arm/joint_states" : "/joint_states")
        : undefined,
      images,
    },
    datasetName,
    capabilities: {
      hasArm,
      canNavigate,
      canManipulate,
      isAerial,
      isUnderwater,
      isStationary,
      isLegged,
    },
    enriched: false,
    source: "derived",
  };
}

// ── Flagship overrides ───────────────────────────────────────────────────────

/**
 * Hand-tuned precision for marquee robots, merged over the derived config.
 * Partial — only the fields we want to pin; everything else stays derived.
 */
export const FLAGSHIP_OVERRIDES: Record<string, Partial<RobotConfig>> = {
  // OhhO reference robot — locked to the exact 9-DOF mobile-manipulation stack
  // every console was historically hardcoded to, so OmniBot looks unchanged.
  omnibot: {
    joints: SO101_JOINTS,
    armDof: 6,
    totalDof: 9,
    stateDim: 9,
    actionDim: 9,
    maxLinVel: 0.2,
    maxAngVel: 1.0,
    compute: { brain: "Raspberry Pi 5 8 GB", accelerator: "NVIDIA GPU (desktop)", vramGb: 16 },
    summary: "OhhO's reference mecanum mobile manipulator: Yahboom X3 base + SO-101 6-DOF arm, Pi 5 brain, full ROS 2 + VLA stack.",
  },
  "omnibot-pro": {
    joints: SO101_JOINTS,
    armDof: 6,
    totalDof: 9,
    stateDim: 9,
    actionDim: 9,
    maxLinVel: 0.2,
    maxAngVel: 1.0,
    compute: { brain: "Raspberry Pi 5 8 GB", accelerator: "NVIDIA GPU (desktop)", vramGb: 16 },
    summary: "Yahboom X3 mecanum base + SO-101 6-DOF arm, RPi 5 brain, depth + wrist cameras, full ROS 2 + VLA stack.",
  },
  "so101-arm": {
    joints: SO101_JOINTS,
    armDof: 6,
    totalDof: 6,
    stateDim: 6,
    actionDim: 6,
    compute: { brain: "Host workstation", accelerator: "NVIDIA GPU", vramGb: 8 },
    summary: "Open-source 3D-printed 6-DOF arm (Feetech STS3215). 0.62 m reach, 0.5 kg payload — the OhhO reference manipulator.",
  },
  turtlebot4: {
    maxLinVel: 0.31,
    maxAngVel: 1.9,
    compute: { brain: "Raspberry Pi 4 4 GB", accelerator: "OAK-D (Myriad X)" },
    summary: "The standard ROS 2 education/research differential base: iRobot Create 3, RPi 4, OAK-D camera, RPLIDAR.",
  },
  ur5e: {
    armDof: 6,
    totalDof: 6,
    stateDim: 6,
    actionDim: 6,
    joints: [
      { name: "shoulder_pan_joint", label: "Base", min: -6.28, max: 6.28, home: 0 },
      { name: "shoulder_lift_joint", label: "Shoulder", min: -6.28, max: 6.28, home: -1.57 },
      { name: "elbow_joint", label: "Elbow", min: -3.14, max: 3.14, home: 1.57 },
      { name: "wrist_1_joint", label: "Wrist 1", min: -6.28, max: 6.28, home: -1.57 },
      { name: "wrist_2_joint", label: "Wrist 2", min: -6.28, max: 6.28, home: -1.57 },
      { name: "wrist_3_joint", label: "Wrist 3", min: -6.28, max: 6.28, home: 0 },
    ],
    compute: { brain: "UR control box + host PC", accelerator: "NVIDIA GPU", vramGb: 8 },
    summary: "Universal Robots UR5e: 6-DOF cobot, 5 kg payload, 850 mm reach, ±0.03 mm repeatability. ROS 2 via ur_robot_driver.",
  },
  "unitree-go2": {
    armDof: 0,
    totalDof: 12,
    stateDim: 12,
    actionDim: 12,
    maxLinVel: 5.0,
    maxAngVel: 2.0,
    compute: { brain: "Onboard 8-core + NX", accelerator: "NVIDIA Jetson" },
    summary: "Unitree Go2 quadruped: 12-DOF legs, 4D LiDAR, ROS 2 SDK with full walking/trotting gaits.",
  },
  "px4-quad": {
    compute: { brain: "Pixhawk + RPi 5 / Jetson", accelerator: "NVIDIA Jetson" },
    summary: "Open-source PX4/ArduPilot quadcopter with a ROS 2 companion computer — build-your-own aerial platform.",
  },
  bluerov2: {
    maxLinVel: 1.5,
    summary: "Blue Robotics BlueROV2: 6-thruster ROV, HD camera, 100 m depth, ROS 2 via ArduSub.",
  },
  "unitree-g1": {
    armDof: 14,
    summary: "Unitree G1 compact humanoid: 23–43 DOF, 3-finger dexterous hands, 14 cm depth camera.",
  },
};

// ── Public lookup + merge ────────────────────────────────────────────────────

function applyOverride(base: RobotConfig): RobotConfig {
  const ov = FLAGSHIP_OVERRIDES[base.robotId];
  if (!ov) return base;
  const merged: RobotConfig = { ...base, ...ov, source: "flagship" };
  // Keep derived dims coherent when an override pins armDof but not the dims.
  if (ov.armDof != null && ov.totalDof == null) {
    merged.totalDof = merged.armDof + merged.baseDof;
    merged.stateDim = merged.totalDof;
    merged.actionDim = merged.totalDof;
  }
  return merged;
}

/** Get the full config for a hardware model id (derived + flagship override). */
export function getRobotConfig(hardwareModelId: string): RobotConfig | undefined {
  const hw = getHardwareModel(hardwareModelId);
  const type = getRobotTypeForHardware(hardwareModelId);
  if (!hw || !type) return undefined;
  const category = getCategory(type.category);
  return applyOverride(deriveRobotConfig(hw, type, category.id));
}

/** OmniBot reference config — the fallback when no robot is selected. */
export function defaultRobotConfig(): RobotConfig {
  return (
    getRobotConfig("omnibot") ??
    getRobotConfig("omnibot-pro") ?? {
      // Last-resort literal so consoles never crash if the catalog changes.
      robotId: "omnibot",
      name: "OmniBot Pro",
      manufacturer: "OhhO",
      category: "mobile-manipulator",
      drive: "mecanum",
      driveLabel: "Mecanum (holonomic)",
      holonomic: true,
      baseDof: 3,
      baseActionLabels: ["vx", "vy", "ω"],
      hasArm: true,
      armDof: 6,
      joints: SO101_JOINTS,
      totalDof: 9,
      stateDim: 9,
      actionDim: 9,
      sensors: deriveSensors("mobile-manipulator", "mecanum", true, {}),
      compute: { brain: "Raspberry Pi 5 8 GB", accelerator: "NVIDIA GPU (desktop)", vramGb: 16 },
      payloadKg: 0.5,
      maxLinVel: 0.2,
      maxAngVel: 1.0,
      ros: "ros2",
      rosTopics: {
        cmdVel: "/cmd_vel",
        odom: "/odom",
        jointStates: "/arm/joint_states",
        images: ["/camera/front/image_raw", "/camera/wrist/image_raw", "/camera/base/bev/image_raw"],
      },
      datasetName: "local/mobile_manipulation",
      capabilities: {
        hasArm: true,
        canNavigate: true,
        canManipulate: true,
        isAerial: false,
        isUnderwater: false,
        isStationary: false,
        isLegged: false,
      },
      enriched: false,
      source: "flagship",
    }
  );
}
