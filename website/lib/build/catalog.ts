/*
 * OhhO Build — parts catalog.
 *
 * The single source of truth for every component a designer can drop onto the
 * canvas. Each part carries REAL engineering specs (mass, price, supplier, lead
 * time, plus category-specific numbers) so the validation engine in
 * lib/build/engine.ts can check a design against physical constraints, and the
 * exporters in lib/build/exporters.ts can emit an order-ready bill of materials.
 *
 * Pure data + types only — no React, no client state — so it can be imported
 * from server components, client components and plain modules alike.
 */

export type Category =
  | "base"
  | "drive"
  | "power"
  | "compute"
  | "arm"
  | "gripper"
  | "sensor";

/** Operating environments a part can be rated for. */
export type Environment =
  | "indoor"
  | "outdoor"
  | "cleanroom"
  | "cold-chain"
  | "agriculture";

export interface Part {
  id: string;
  name: string;
  category: Category;
  brand: string;
  desc: string;

  /** Physical + commercial facts (every part has these). */
  mass: number; // kg
  price: number; // USD
  leadTimeDays: number;
  supplier: string;
  supplierUrl: string;
  /** Bounding size in metres, [length(x), width(y), height(z)]. */
  size: [number, number, number];
  /** Environments this part is rated for. Defaults to indoor-only. */
  env: Environment[];

  // ── category-specific specs (optional) ──────────────────────────────────
  payload?: number; // kg — base: rated carry capacity; arm/gripper: lift at tip
  topSpeed?: number; // m/s — drive
  reach?: number; // m — arm
  dof?: number; // arm degrees of freedom
  voltage?: number; // V — power nominal pack voltage
  capacityWh?: number; // Wh — power energy
  powerDraw?: number; // W — average electrical draw (drive/compute/sensor/arm)
  tops?: number; // AI TOPS — compute
  range?: number; // m — sensor sensing range
  fov?: number; // deg — sensor field of view
}

export interface CategoryMeta {
  key: Category;
  label: string;
  /** Single-select (replaces) vs multi-select (toggle, e.g. sensors). */
  single: boolean;
  /** Required for a valid robot. */
  required: boolean;
  blurb: string;
}

export const CATEGORIES: CategoryMeta[] = [
  { key: "base", label: "Base", single: true, required: true, blurb: "Chassis the robot is built on" },
  { key: "drive", label: "Drive", single: true, required: true, blurb: "Wheels & motors" },
  { key: "power", label: "Power", single: true, required: true, blurb: "Battery pack" },
  { key: "compute", label: "Compute", single: true, required: true, blurb: "On-board brain" },
  { key: "arm", label: "Arm", single: true, required: false, blurb: "Manipulator (optional)" },
  { key: "gripper", label: "Gripper", single: true, required: false, blurb: "End effector (needs an arm)" },
  { key: "sensor", label: "Sensors", single: false, required: false, blurb: "Cameras, LiDAR, IMU" },
];

export const ENVIRONMENTS: { key: Environment; label: string }[] = [
  { key: "indoor", label: "Indoor" },
  { key: "outdoor", label: "Outdoor" },
  { key: "cleanroom", label: "Cleanroom" },
  { key: "cold-chain", label: "Cold-chain" },
  { key: "agriculture", label: "Agriculture" },
];

// ── The catalog ────────────────────────────────────────────────────────────
// Several alternatives per category so the recommendation engine has real swaps
// to suggest. OmniBot's own reference hardware is included so a design here maps
// straight onto the rest of the stack.

export const PARTS: Part[] = [
  // ── BASE ──────────────────────────────────────────────────────────────────
  {
    id: "base-yahboom-x3",
    name: "Yahboom Mecanum X3",
    category: "base",
    brand: "Yahboom",
    desc: "OmniBot's reference mecanum chassis — omnidirectional, compact, indoor.",
    mass: 1.8,
    price: 220,
    leadTimeDays: 7,
    supplier: "Yahboom",
    supplierUrl: "https://category.yahboom.net/",
    size: [0.265, 0.25, 0.13],
    env: ["indoor"],
    payload: 5,
  },
  {
    id: "base-nano",
    name: "NanoBase Mini",
    category: "base",
    brand: "OhhO Parts",
    desc: "Palm-sized differential base for benchtop and education robots.",
    mass: 0.9,
    price: 95,
    leadTimeDays: 5,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.18, 0.16, 0.09],
    env: ["indoor", "cleanroom"],
    payload: 2,
  },
  {
    id: "base-amr-hd",
    name: "AMR-HD Chassis",
    category: "base",
    brand: "OhhO Parts",
    desc: "Heavy-duty warehouse AMR frame with a steel deck and bumper skirt.",
    mass: 6.0,
    price: 1200,
    leadTimeDays: 21,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.5, 0.45, 0.18],
    env: ["indoor", "outdoor"],
    payload: 25,
  },
  {
    id: "base-ip65",
    name: "FieldFrame IP65",
    category: "base",
    brand: "OhhO Parts",
    desc: "Sealed, gasketed outdoor/cold-chain chassis with a wash-down rating.",
    mass: 7.5,
    price: 1800,
    leadTimeDays: 30,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.55, 0.5, 0.2],
    env: ["indoor", "outdoor", "cleanroom", "cold-chain", "agriculture"],
    payload: 20,
  },

  // ── DRIVE ─────────────────────────────────────────────────────────────────
  {
    id: "drive-mecanum-4",
    name: "Mecanum ×4 · STS",
    category: "drive",
    brand: "Yahboom",
    desc: "Four 40 mm mecanum wheels on geared STS servos — true omnidirectional.",
    mass: 0.6,
    price: 140,
    leadTimeDays: 7,
    supplier: "Yahboom",
    supplierUrl: "https://category.yahboom.net/",
    size: [0.08, 0.08, 0.08],
    env: ["indoor"],
    topSpeed: 1.2,
    powerDraw: 36,
  },
  {
    id: "drive-diff-2",
    name: "Diff-Drive ×2",
    category: "drive",
    brand: "OhhO Parts",
    desc: "Two driven hub wheels plus a caster — simple, cheap, benchtop-friendly.",
    mass: 0.4,
    price: 70,
    leadTimeDays: 5,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.07, 0.07, 0.07],
    env: ["indoor", "cleanroom"],
    topSpeed: 0.9,
    powerDraw: 22,
  },
  {
    id: "drive-mecanum-hd",
    name: "Mecanum ×4 · BLDC",
    category: "drive",
    brand: "OhhO Parts",
    desc: "100 mm mecanum wheels on brushless hub motors for laden AMRs.",
    mass: 2.4,
    price: 640,
    leadTimeDays: 21,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.12, 0.12, 0.12],
    env: ["indoor", "outdoor"],
    topSpeed: 1.8,
    powerDraw: 120,
  },
  {
    id: "drive-offroad-4",
    name: "All-Terrain ×4",
    category: "drive",
    brand: "OhhO Parts",
    desc: "Lugged rubber tyres on sealed hub motors for outdoor and field use.",
    mass: 3.2,
    price: 900,
    leadTimeDays: 28,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.16, 0.16, 0.16],
    env: ["indoor", "outdoor", "agriculture"],
    topSpeed: 2.4,
    powerDraw: 180,
  },

  // ── POWER ─────────────────────────────────────────────────────────────────
  {
    id: "power-3s-5ah",
    name: "LiPo 3S · 5 Ah",
    category: "power",
    brand: "OhhO Parts",
    desc: "11.1 V, 55 Wh — light pack for benchtop and education robots.",
    mass: 0.32,
    price: 45,
    leadTimeDays: 5,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.1, 0.04, 0.03],
    env: ["indoor", "cleanroom"],
    voltage: 11.1,
    capacityWh: 55,
  },
  {
    id: "power-4s-10ah",
    name: "LiPo 4S · 10 Ah",
    category: "power",
    brand: "OhhO Parts",
    desc: "14.8 V, 148 Wh — the OmniBot workhorse pack for a full shift of teleop.",
    mass: 0.78,
    price: 120,
    leadTimeDays: 7,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.14, 0.05, 0.04],
    env: ["indoor"],
    voltage: 14.8,
    capacityWh: 148,
  },
  {
    id: "power-liion-24",
    name: "Li-ion 7S · 20 Ah",
    category: "power",
    brand: "OhhO Parts",
    desc: "24 V, 480 Wh — long-runtime pack for laden AMRs.",
    mass: 2.6,
    price: 320,
    leadTimeDays: 14,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.18, 0.1, 0.07],
    env: ["indoor", "outdoor"],
    voltage: 24,
    capacityWh: 480,
  },
  {
    id: "power-lfp-48",
    name: "LiFePO₄ 48 V · 30 Ah",
    category: "power",
    brand: "OhhO Parts",
    desc: "48 V, 1.44 kWh sealed pack — all-day field and outdoor missions.",
    mass: 9.5,
    price: 780,
    leadTimeDays: 21,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.24, 0.16, 0.12],
    env: ["indoor", "outdoor", "cold-chain", "agriculture"],
    voltage: 48,
    capacityWh: 1440,
  },

  // ── COMPUTE ───────────────────────────────────────────────────────────────
  {
    id: "compute-pi5",
    name: "Raspberry Pi 5 · 8 GB",
    category: "compute",
    brand: "Raspberry Pi",
    desc: "OmniBot's robot brain — runs every ROS 2 node except the heavy VLA.",
    mass: 0.06,
    price: 80,
    leadTimeDays: 7,
    supplier: "Raspberry Pi",
    supplierUrl: "https://www.raspberrypi.com/products/raspberry-pi-5/",
    size: [0.085, 0.056, 0.018],
    env: ["indoor"],
    powerDraw: 12,
    tops: 0,
  },
  {
    id: "compute-orin-nano",
    name: "Jetson Orin Nano",
    category: "compute",
    brand: "NVIDIA",
    desc: "40 TOPS edge AI — runs SmolVLA / ACT policies on the robot itself.",
    mass: 0.18,
    price: 499,
    leadTimeDays: 14,
    supplier: "NVIDIA",
    supplierUrl: "https://developer.nvidia.com/embedded/jetson-orin",
    size: [0.103, 0.09, 0.04],
    env: ["indoor", "outdoor"],
    powerDraw: 15,
    tops: 40,
  },
  {
    id: "compute-agx-orin",
    name: "Jetson AGX Orin",
    category: "compute",
    brand: "NVIDIA",
    desc: "275 TOPS — on-board big-model inference and multi-camera perception.",
    mass: 0.7,
    price: 1999,
    leadTimeDays: 30,
    supplier: "NVIDIA",
    supplierUrl: "https://developer.nvidia.com/embedded/jetson-orin",
    size: [0.11, 0.11, 0.072],
    env: ["indoor", "outdoor"],
    powerDraw: 60,
    tops: 275,
  },
  {
    id: "compute-nuc-gpu",
    name: "Mini-PC + RTX",
    category: "compute",
    brand: "OhhO Parts",
    desc: "x86 mini-PC with a mobile RTX GPU for full OpenVLA-class inference.",
    mass: 1.6,
    price: 1400,
    leadTimeDays: 21,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.12, 0.12, 0.05],
    env: ["indoor"],
    powerDraw: 120,
    tops: 100,
  },

  // ── ARM ───────────────────────────────────────────────────────────────────
  {
    id: "arm-so101",
    name: "SO-101 6-DOF",
    category: "arm",
    brand: "LeRobot",
    desc: "OmniBot's reference arm — six Feetech STS3215 servos, 0.62 m reach.",
    mass: 1.1,
    price: 480,
    leadTimeDays: 14,
    supplier: "LeRobot",
    supplierUrl: "https://github.com/huggingface/lerobot",
    size: [0.08, 0.08, 0.32],
    env: ["indoor"],
    reach: 0.62,
    dof: 6,
    payload: 0.5,
    powerDraw: 18,
  },
  {
    id: "arm-so100-5dof",
    name: "SO-100 5-DOF",
    category: "arm",
    brand: "LeRobot",
    desc: "Lighter five-axis arm for benchtop manipulation and teaching.",
    mass: 0.8,
    price: 320,
    leadTimeDays: 10,
    supplier: "LeRobot",
    supplierUrl: "https://github.com/huggingface/lerobot",
    size: [0.07, 0.07, 0.26],
    env: ["indoor", "cleanroom"],
    reach: 0.5,
    dof: 5,
    payload: 0.35,
    powerDraw: 12,
  },
  {
    id: "arm-cobot-lite",
    name: "Cobot-Lite 6-DOF",
    category: "arm",
    brand: "OhhO Parts",
    desc: "Collaborative-rated arm, 0.85 m reach, 3 kg payload, cleanroom-ready.",
    mass: 11,
    price: 12000,
    leadTimeDays: 45,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.12, 0.12, 0.6],
    env: ["indoor", "cleanroom"],
    reach: 0.85,
    dof: 6,
    payload: 3,
    powerDraw: 90,
  },

  // ── GRIPPER ───────────────────────────────────────────────────────────────
  {
    id: "gripper-parallel",
    name: "Parallel 2-Finger",
    category: "gripper",
    brand: "LeRobot",
    desc: "Servo parallel jaw — the SO-101 default end effector.",
    mass: 0.18,
    price: 90,
    leadTimeDays: 7,
    supplier: "LeRobot",
    supplierUrl: "https://github.com/huggingface/lerobot",
    size: [0.06, 0.08, 0.06],
    env: ["indoor"],
    payload: 0.5,
    powerDraw: 4,
  },
  {
    id: "gripper-soft",
    name: "Soft Compliant Grip",
    category: "gripper",
    brand: "OhhO Parts",
    desc: "Food-safe silicone fingers for delicate or cold-chain handling.",
    mass: 0.12,
    price: 140,
    leadTimeDays: 12,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.07, 0.09, 0.06],
    env: ["indoor", "cleanroom", "cold-chain"],
    payload: 0.3,
    powerDraw: 3,
  },
  {
    id: "gripper-vacuum",
    name: "Vacuum Suction",
    category: "gripper",
    brand: "OhhO Parts",
    desc: "Single-cup vacuum end effector for flat boxes and totes.",
    mass: 0.3,
    price: 260,
    leadTimeDays: 14,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.06, 0.06, 0.1],
    env: ["indoor", "outdoor"],
    payload: 1.2,
    powerDraw: 25,
  },

  // ── SENSORS ───────────────────────────────────────────────────────────────
  {
    id: "sensor-astra",
    name: "Orbbec Astra Depth",
    category: "sensor",
    brand: "Orbbec",
    desc: "RGB-D camera — OmniBot's perception + depth→scan source.",
    mass: 0.3,
    price: 150,
    leadTimeDays: 10,
    supplier: "Orbbec",
    supplierUrl: "https://www.orbbec.com/",
    size: [0.165, 0.04, 0.05],
    env: ["indoor"],
    range: 8,
    fov: 60,
    powerDraw: 2.4,
  },
  {
    id: "sensor-lidar-2d",
    name: "2-D LiDAR",
    category: "sensor",
    brand: "Slamtec",
    desc: "360° planar scanner for SLAM and Nav2 obstacle avoidance.",
    mass: 0.19,
    price: 99,
    leadTimeDays: 7,
    supplier: "Slamtec",
    supplierUrl: "https://www.slamtec.com/",
    size: [0.07, 0.07, 0.06],
    env: ["indoor", "outdoor"],
    range: 12,
    fov: 360,
    powerDraw: 3,
  },
  {
    id: "sensor-lidar-3d",
    name: "3-D LiDAR",
    category: "sensor",
    brand: "OhhO Parts",
    desc: "Spinning 3-D LiDAR for outdoor mapping and long-range perception.",
    mass: 0.83,
    price: 1200,
    leadTimeDays: 30,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.1, 0.1, 0.09],
    env: ["indoor", "outdoor", "agriculture"],
    range: 100,
    fov: 360,
    powerDraw: 8,
  },
  {
    id: "sensor-rgb-front",
    name: "Global-Shutter RGB",
    category: "sensor",
    brand: "OhhO Parts",
    desc: "Forward camera for VLA prompts and operator video.",
    mass: 0.05,
    price: 60,
    leadTimeDays: 5,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.03, 0.03, 0.03],
    env: ["indoor", "outdoor"],
    range: 0,
    fov: 90,
    powerDraw: 1,
  },
  {
    id: "sensor-wrist-cam",
    name: "Wrist Camera",
    category: "sensor",
    brand: "OhhO Parts",
    desc: "Eye-in-hand camera for SmolVLA manipulation policies.",
    mass: 0.04,
    price: 45,
    leadTimeDays: 5,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.025, 0.025, 0.025],
    env: ["indoor", "cleanroom"],
    range: 0,
    fov: 70,
    powerDraw: 1,
  },
  {
    id: "sensor-imu",
    name: "9-DOF IMU",
    category: "sensor",
    brand: "OhhO Parts",
    desc: "Accel + gyro + mag for EKF odometry fusion.",
    mass: 0.02,
    price: 25,
    leadTimeDays: 3,
    supplier: "OhhO Parts",
    supplierUrl: "https://ohho.ai/",
    size: [0.02, 0.02, 0.008],
    env: ["indoor", "outdoor", "cleanroom", "cold-chain", "agriculture"],
    powerDraw: 0.1,
  },
];

// ── Lookups ─────────────────────────────────────────────────────────────────

const BY_ID: Record<string, Part> = Object.fromEntries(PARTS.map((p) => [p.id, p]));

export function getPart(id: string): Part | undefined {
  return BY_ID[id];
}

export function partsByCategory(cat: Category): Part[] {
  return PARTS.filter((p) => p.category === cat);
}

export function categoryMeta(cat: Category): CategoryMeta {
  return CATEGORIES.find((c) => c.key === cat)!;
}
