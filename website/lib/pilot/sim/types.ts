/*
 * OhhO Pilot — Simulator provider interface.
 *
 * Each provider implements a standard interface so the Pilot console can
 * switch between simulated, Gazebo, Isaac Sim, MuJoCo, etc. without changing
 * any UI code. Providers are tiered: free (included on every plan) vs pro
 * (requires Fleet or Forge plan).
 *
 * Recording: any provider can record episodes in LeRobot-compatible format.
 * Start/stop captures a complete 9-DOF mobile-manipulation episode with
 * multi-camera frames, joint states, odometry, and actions.
 */

/* ══════════════════════════════════════════════════════════════════════════
   Types
   ══════════════════════════════════════════════════════════════════════════ */

export interface Velocity {
  linearX: number;
  linearY: number;
  angularZ: number;
}

export interface Odometry {
  x: number;
  y: number;
  theta: number;
  linearX: number;
  linearY: number;
  angularZ: number;
  timestamp: number;
}

export interface JointState {
  name: string;
  position: number;
  velocity: number;
  effort: number;
}

export interface SimulatedFrame {
  /** Camera label (e.g. "front", "wrist", "BEV"). */
  camera: string;
  /** Base64-encoded JPEG frame. */
  base64: string;
  /** Timestamp, ms. */
  timestamp: number;
  /** Simulated scene objects (bounding boxes for overlay). */
  objects?: SceneObject[];
}

export interface SceneObject {
  label: string;
  x: number;
  y: number;
  hw: number;
  hh: number;
  confidence: number;
  color: string;
}

export interface SimulatorStatus {
  connected: boolean;
  connecting: boolean;
  /** Human-readable status line. */
  label: string;
  /** Latency to the simulator, ms (0 if simulated). */
  latency: number;
  /** Frames per second being received. */
  fps: number;
  /** Last error, if any. */
  error?: string;
}

export type SimTier = "free" | "pro" | "enterprise";

export interface SimProviderMeta {
  id: string;
  name: string;
  desc: string;
  tier: SimTier;
  /** Requires an external server (not in-browser). */
  requiresServer: boolean;
  /** Default connection URL / port. */
  defaultUrl: string;
  /** Setup instructions shown when unavailable. */
  setupHint: string;
  /** Whether this provider supports recording. */
  supportsRecording: boolean;
  /** Whether this provider is available (vs "coming soon"). */
  available: boolean;
}

/** All registered simulation providers in tier order. */
export const SIM_PROVIDERS: SimProviderMeta[] = [
  {
    id: "simulated",
    name: "Simulated (Built-in)",
    desc: "Deterministic browser-side simulation — always works, no external dependencies.",
    tier: "free",
    requiresServer: false,
    defaultUrl: "",
    setupHint: "Always available. No setup needed.",
    supportsRecording: true,
    available: true,
  },
  {
    id: "three-physics",
    name: "Three.js 3-D Physics",
    desc: "In-browser 3-D robot simulation with physics — real 3-D scene, friction, velocity integration.",
    tier: "free",
    requiresServer: false,
    defaultUrl: "",
    setupHint: "Always available. Uses built-in Three.js renderer for 3-D scene visualization.",
    supportsRecording: true,
    available: true,
  },
  {
    id: "hybrid",
    name: "Hybrid ROSBridge Sim",
    desc: "Full simulated ROS message bus. Toggle bridge to real rosbridge when a robot is connected.",
    tier: "free",
    requiresServer: false,
    defaultUrl: "",
    setupHint: "Always available. Toggle 'bridge to real' and enter a rosbridge URL to connect to a robot.",
    supportsRecording: true,
    available: true,
  },
  {
    id: "gazebo",
    name: "Gazebo Harmonic",
    desc: "Open-source 3-D robot simulator with physics. Connect via ROSBridge on port 9090.",
    tier: "free",
    requiresServer: true,
    defaultUrl: "ws://localhost:9090",
    setupHint: "Launch: ./launch_simulation.sh or ros2 launch omnibot_bringup simulation.launch.py. Then start rosbridge: ./launch_rosbridge.sh",
    supportsRecording: true,
    available: true,
  },
  {
    id: "isaac",
    name: "NVIDIA Isaac Sim",
    desc: "Photorealistic simulation with PhysX 5, RTX rendering, domain randomization, and synthetic data generation. Best for training data at scale.",
    tier: "pro",
    requiresServer: true,
    defaultUrl: "ws://localhost:9090",
    setupHint: "Launch Isaac Sim with the OmniBot USD scene. Requires NVIDIA GPU (RTX 3060+). Enable the ROS 2 Bridge extension and Isaac Sim rosbridge.",
    supportsRecording: true,
    available: false,
  },
  {
    id: "mujoco",
    name: "MuJoCo (DeepMind)",
    desc: "Fast, accurate physics engine for robotics research. Excellent for RL training and sim-to-real transfer.",
    tier: "pro",
    requiresServer: true,
    defaultUrl: "ws://localhost:9090",
    setupHint: "Install mujoco and mujoco_ros2. Requires a running MuJoCo simulation with the ROS 2 bridge plugin enabled.",
    supportsRecording: true,
    available: false,
  },
  {
    id: "isim4",
    name: "NVIDIA Isaac Sim 4.0",
    desc: "Next-gen Isaac Sim on Omniverse — multi-GPU, multi-robot, cloud-scalable, with RTX neural rendering.",
    tier: "enterprise",
    requiresServer: true,
    defaultUrl: "ws://localhost:9090",
    setupHint: "Requires Omniverse Enterprise license. Contact sales for cloud deployment and multi-robot scaling.",
    supportsRecording: true,
    available: false,
  },
];

/**
 * Find a provider by id. Returns the first (simulated) if not found.
 */
export function getSimProvider(id: string): SimProviderMeta {
  return SIM_PROVIDERS.find((p) => p.id === id) ?? SIM_PROVIDERS[0];
}

/**
 * Check if a tier is accessible on a given plan.
 */
export function tierAccessible(
  tier: SimTier,
  plan: string,
): { allowed: boolean; badge: string; upgradeMessage?: string } {
  switch (plan) {
    case "Spark":
      return {
        allowed: tier === "free",
        badge: tier === "free" ? "Included" : "Upgrade",
        upgradeMessage: tier !== "free" ? "Pro simulators require Builder plan or above." : undefined,
      };
    case "Builder":
      return {
        allowed: tier !== "enterprise",
        badge: tier === "enterprise" ? "Forge only" : tier === "pro" ? "Pro" : "Included",
        upgradeMessage: tier === "enterprise" ? "Enterprise sims require Forge plan." : undefined,
      };
    case "Fleet":
      return {
        allowed: tier !== "enterprise",
        badge: tier === "enterprise" ? "Forge only" : tier === "pro" ? "Pro" : "Included",
        upgradeMessage: tier === "enterprise" ? "Enterprise sims require Forge plan." : undefined,
      };
    case "Forge":
      return { allowed: true, badge: tier === "enterprise" ? "Enterprise" : tier === "pro" ? "Pro" : "Included" };
    default:
      return { allowed: tier === "free", badge: tier === "free" ? "Included" : "Upgrade" };
  }
}

/* ══════════════════════════════════════════════════════════════════════════
   Simulator Provider Interface (abstract — each implementation fills this)
   ══════════════════════════════════════════════════════════════════════════ */

export interface SimulatorProvider {
  readonly meta: SimProviderMeta;

  /** Connect to the simulator. url may override the default. */
  connect(url?: string): Promise<SimulatorStatus>;

  /** Disconnect and clean up. */
  disconnect(): void;

  /** Current connection status. */
  getStatus(): SimulatorStatus;

  /** Send a velocity command (mapped to /cmd_vel). */
  sendVelocity(vel: Velocity): void;

  /** Send a joint position command (radians). */
  sendJointCommand(jointName: string, position: number): void;

  /** Emergency stop — zero all velocities, freeze arm. */
  emergencyStop(): void;

  /** Release e-stop. */
  releaseStop(): void;

  /** Subscribe to odometry updates. Returns unsubscribe function. */
  onOdometry(cb: (odom: Odometry) => void): () => void;

  /** Subscribe to joint state updates. Returns unsubscribe function. */
  onJointStates(cb: (joints: JointState[]) => void): () => void;

  /** Subscribe to camera frames. Returns unsubscribe function. */
  onCameraFrame(camera: string, cb: (frame: SimulatedFrame) => void): () => void;

  /** Subscribe to status changes. Returns unsubscribe function. */
  onStatusChange(cb: (status: SimulatorStatus) => void): () => void;

  /* ── Recording ─────────────────────────────────────────────────────── */

  /** Begin recording an episode. Requires the provider to be connected. */
  startRecording(instruction?: string): void;

  /** Stop recording. Returns the episode metadata. */
  stopRecording(): RecordedEpisode | null;

  /** Whether a recording is in progress. */
  isRecording(): boolean;

  /** Download the last recorded episode as a LeRobot-compatible CSV. */
  downloadLastEpisode(): void;

  /** Optional: return 3-D scene objects for rendering (Three.js providers). */
  getSceneObjects?(): ThreeSceneState | null;
}

export interface ThreeSceneState {
  /** Robot base pose in world coordinates. */
  robot: {
    x: number; y: number; z: number; theta: number;
    length: number; width: number; height: number;
  };
  /** Arm joints in radians (6-DOF). */
  armJoints: number[];
  /** Static obstacles in the scene. */
  obstacles: Array<{ x: number; y: number; w: number; h: number; color: string }>;
  /** Camera position for rendering. */
  camera: { x: number; y: number; z: number; lookAt: [number, number, number] };
}

export interface RecordedEpisode {
  id: string;
  instruction: string;
  provider: string;
  startedAt: number;
  durationMs: number;
  frameCount: number;
  cameras: string[];
  /** CSV content for LeRobot-format export. */
  exportContent: string;
}

export interface RecordingFrame {
  timestamp: number;
  /** 6 arm joints in radians. */
  armPositions: number[];
  /** 3 base velocities (vx, vy, ω). */
  baseVelocity: number[];
  /** Camera frame per camera id → base64 JPEG. */
  cameraFrames: Record<string, string>;
}
