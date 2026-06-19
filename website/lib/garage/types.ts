/*
 * Garage types — the single source of truth for robot categories, robot models,
 * hardware variants, and user garage entries across the OhhO platform.
 *
 * Pure data + types only — no React, no client state — so it can be imported
 * from server components, client components, and plain modules alike.
 */

// ── Top-level robot categories ───────────────────────────────────────────────

export type RobotCategoryId =
  | "drones"
  | "wheeled"
  | "legged"
  | "humanoid"
  | "tracked"
  | "marine"
  | "industrial-arm"
  | "mobile-manipulator"
  | "swarm"
  | "agricultural"
  | "underwater-rov"
  | "space"
  | "medical"
  | "delivery"
  | "inspection";

export interface RobotCategory {
  id: RobotCategoryId;
  label: string;
  icon: string; // lucide icon name
  blurb: string;
  color: string; // tailwind-ish hex for accent
}

// ── A specific robot hardware model ──────────────────────────────────────────

export interface HardwareModel {
  id: string;
  name: string;
  manufacturer: string;
  desc: string;
  specs: Record<string, string>;
  /** Image URL or placeholder. */
  imageUrl: string;
  /** Rough USD price (when available). */
  price: string;
  /** What OhhO products are pre-configured for this model. */
  supportedProducts: string[];
  /** ROS / ROS 2 compatibility. */
  ros: "ros1" | "ros2" | "both" | "none" | "custom";
  /** Locomotion type. */
  locomotion: string;
  /** Has a manipulator arm? */
  hasArm: boolean;
  /** Max payload in kg (0 if N/A). */
  payloadKg: number;
  /** Approx weight in kg. */
  weightKg: number;
}

// ── A robot type definition (groups hardware models) ─────────────────────────

export interface RobotType {
  id: string;
  name: string;
  category: RobotCategoryId;
  tagline: string;
  description: string;
  /** Pre-loaded hardware models for this type. */
  hardwareModels: HardwareModel[];
}

// ── A user's robot in their garage ───────────────────────────────────────────

export type RobotStatus = "active" | "draft" | "simulated" | "offline";

export interface UserRobot {
  id: string;
  userId: string;
  name: string;
  robotTypeId: string;
  hardwareModelId: string;
  status: RobotStatus;
  /** JSON blob for robot-specific configuration. */
  config: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

// ── Full robot details for display (joining UserRobot + catalog data) ───────

export interface GarageRobot {
  userRobot: UserRobot;
  robotType: RobotType;
  hardwareModel: HardwareModel;
  category: RobotCategory;
}

// ── Categories lookup ────────────────────────────────────────────────────────

export const CATEGORIES: RobotCategory[] = [
  {
    id: "drones",
    label: "Drones",
    icon: "Drone",
    blurb: "Aerial robots — quadcopters, hexacopters, fixed-wing, and VTOL UAVs.",
    color: "#00d4ff",
  },
  {
    id: "wheeled",
    label: "Wheeled Robots",
    icon: "Car",
    blurb: "Differential, mecanum, ackermann, and omnidirectional ground vehicles.",
    color: "#a78bfa",
  },
  {
    id: "legged",
    label: "Legged Robots",
    icon: "Footprints",
    blurb: "Quadrupeds, hexapods, and other walking robots.",
    color: "#f97316",
  },
  {
    id: "humanoid",
    label: "Humanoid Robots",
    icon: "User",
    blurb: "Bipedal humanoid robots — general-purpose embodied AI platforms.",
    color: "#34d399",
  },
  {
    id: "tracked",
    label: "Tracked Robots",
    icon: "LayoutList",
    blurb: "Tank-tread robots for rough terrain and heavy payloads.",
    color: "#fbbf24",
  },
  {
    id: "marine",
    label: "Marine Robots",
    icon: "Ship",
    blurb: "Autonomous surface vessels and unmanned boats.",
    color: "#38bdf8",
  },
  {
    id: "industrial-arm",
    label: "Industrial Arms",
    icon: "Armchair",
    blurb: "Fixed-base robotic arms — manufacturing, assembly, and lab automation.",
    color: "#fb7185",
  },
  {
    id: "mobile-manipulator",
    label: "Mobile Manipulators",
    icon: "Bot",
    blurb: "Wheeled bases with arms — the full embodied-AI platform.",
    color: "#818cf8",
  },
  {
    id: "swarm",
    label: "Swarm Robots",
    icon: "Grid3x3",
    blurb: "Multi-agent swarms for collective intelligence and distributed tasks.",
    color: "#22d3ee",
  },
  {
    id: "agricultural",
    label: "Agricultural",
    icon: "Leaf",
    blurb: "Precision farming robots — planting, weeding, harvesting, and monitoring.",
    color: "#4ade80",
  },
  {
    id: "underwater-rov",
    label: "Underwater ROVs",
    icon: "Waves",
    blurb: "Remotely operated and autonomous underwater vehicles.",
    color: "#0ea5e9",
  },
  {
    id: "space",
    label: "Space Robotics",
    icon: "Rocket",
    blurb: "Orbital and planetary robots — rovers, satellite servicers, and landers.",
    color: "#e2e8f0",
  },
  {
    id: "medical",
    label: "Medical Robots",
    icon: "HeartPulse",
    blurb: "Surgical, rehabilitation, and hospital-assistance robots.",
    color: "#f472b6",
  },
  {
    id: "delivery",
    label: "Delivery Robots",
    icon: "Package",
    blurb: "Last-mile autonomous delivery platforms — sidewalks to roads.",
    color: "#a3e635",
  },
  {
    id: "inspection",
    label: "Inspection Robots",
    icon: "Search",
    blurb: "Pipeline, infrastructure, and confined-space inspection robots.",
    color: "#c084fc",
  },
];

export function getCategory(id: RobotCategoryId): RobotCategory {
  return CATEGORIES.find((c) => c.id === id) ?? CATEGORIES[0];
}
