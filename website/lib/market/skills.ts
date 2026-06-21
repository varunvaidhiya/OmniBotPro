/*
 * OhhO Market — skill catalog, proof results, and author data.
 *
 * Pure data (no React) so it imports from server and client alike.
 * The Market console reads this to render skill listings, the detail
 * view with Proof verification results, author profiles, and deploy actions.
 */

export interface SkillListing {
  id: string;
  name: string;
  description: string;
  /** Robot models this skill runs on. */
  robots: string[];
  /** Robot brand(s). */
  brands: string[];
  /** Locomotion category — humanoid, quadruped, wheeled, arm. */
  category: string;
  /** Training method — SmolVLA, ACT, diffusion, RL, etc. */
  method: string;
  /** Number of training episodes. */
  episodes: number;
  /** Measured success rate from OhhO Proof (0..1). */
  successRate: number;
  /** Price in USD (0 = free). */
  price: number;
  /** Version tag. */
  version: string;
  /** Author ID. */
  authorId: string;
  /** Is the skill signed by OhhO Shield? */
  signed: boolean;
  /** Proof scenario results. */
  proof: ProofScenario[];
  /** Total downloads. */
  downloads: number;
  /** Star rating (0..5). */
  rating: number;
  accent: string;
  tags: string[];
}

export interface ProofScenario {
  suite: string;
  passRate: number;
  runs: number;
}

export interface Author {
  id: string;
  name: string;
  avatar: string;
  skillCount: number;
  totalDownloads: number;
  revenue: number;
  rating: number;
  verified: boolean;
}

export const AUTHORS: Author[] = [
  {
    id: "robotics-lab",
    name: "robotics-lab",
    avatar: "RL",
    skillCount: 12,
    totalDownloads: 8420,
    revenue: 4820,
    rating: 4.9,
    verified: true,
  },
  {
    id: "embodied-ai",
    name: "embodied-ai",
    avatar: "EA",
    skillCount: 8,
    totalDownloads: 5210,
    revenue: 3140,
    rating: 4.7,
    verified: true,
  },
  {
    id: "factory-bot",
    name: "factory-bot",
    avatar: "FB",
    skillCount: 5,
    totalDownloads: 3180,
    revenue: 8900,
    rating: 4.8,
    verified: true,
  },
  {
    id: "community-dev",
    name: "community-dev",
    avatar: "CD",
    skillCount: 3,
    totalDownloads: 1240,
    revenue: 0,
    rating: 4.5,
    verified: false,
  },
];

export const SKILLS: SkillListing[] = [
  {
    id: "pick-place-cup",
    name: "pick-place-cup",
    description: "Pick up a cup from a flat surface and place it at a target location. Trained on 1K+ teleop episodes with a Unitree G1.",
    robots: ["G1", "G1-EDU"],
    brands: ["Unitree"],
    category: "humanoid",
    method: "SmolVLA",
    episodes: 1043,
    successRate: 0.94,
    price: 49,
    version: "v2.1.0",
    authorId: "robotics-lab",
    signed: true,
    downloads: 1248,
    rating: 4.9,
    accent: "#00D4FF",
    tags: ["manipulation", "grasping", "household"],
    proof: [
      { suite: "Navigation", passRate: 0.96, runs: 500 },
      { suite: "Manipulation", passRate: 0.94, runs: 800 },
      { suite: "Edge cases", passRate: 0.87, runs: 300 },
      { suite: "Fault injection", passRate: 0.82, runs: 200 },
    ],
  },
  {
    id: "patrol-warehouse",
    name: "patrol-warehouse",
    description: "Autonomous warehouse patrol with obstacle avoidance and dock return. Optimized for Unitree Go2 quadruped.",
    robots: ["Go2", "B2"],
    brands: ["Unitree"],
    category: "quadruped",
    method: "RL (PPO)",
    episodes: 0,
    successRate: 0.97,
    price: 29,
    version: "v1.3.0",
    authorId: "embodied-ai",
    signed: true,
    downloads: 2140,
    rating: 4.8,
    accent: "#A78BFA",
    tags: ["navigation", "patrol", "industrial"],
    proof: [
      { suite: "Navigation", passRate: 0.97, runs: 1200 },
      { suite: "Obstacle avoidance", passRate: 0.95, runs: 600 },
      { suite: "Edge cases", passRate: 0.91, runs: 400 },
    ],
  },
  {
    id: "weld-seam-v2",
    name: "weld-seam-v2",
    description: "Precision weld-seam following for UR5e with force-torque sensing. Trained via imitation learning on expert demonstrations.",
    robots: ["UR5e", "UR10e"],
    brands: ["Universal Robots"],
    category: "arm",
    method: "ACT",
    episodes: 820,
    successRate: 0.89,
    price: 99,
    version: "v2.0.0",
    authorId: "factory-bot",
    signed: true,
    downloads: 480,
    rating: 4.7,
    accent: "#FBBF24",
    tags: ["welding", "industrial", "precision"],
    proof: [
      { suite: "Trajectory accuracy", passRate: 0.93, runs: 300 },
      { suite: "Force control", passRate: 0.88, runs: 250 },
      { suite: "Edge cases", passRate: 0.82, runs: 150 },
    ],
  },
  {
    id: "tidy-kitchen",
    name: "tidy-kitchen",
    description: "Open-ended kitchen tidying — picks up objects and places them in designated zones. Uses VLA with language grounding.",
    robots: ["G1", "G1-EDU", "H1"],
    brands: ["Unitree"],
    category: "humanoid",
    method: "OpenVLA",
    episodes: 2400,
    successRate: 0.86,
    price: 79,
    version: "v1.0.0",
    authorId: "robotics-lab",
    signed: true,
    downloads: 890,
    rating: 4.6,
    accent: "#00D4FF",
    tags: ["manipulation", "language-grounded", "household"],
    proof: [
      { suite: "Object detection", passRate: 0.92, runs: 600 },
      { suite: "Grasping", passRate: 0.88, runs: 800 },
      { suite: "Placement", passRate: 0.84, runs: 500 },
      { suite: "Edge cases", passRate: 0.79, runs: 300 },
    ],
  },
  {
    id: "door-open",
    name: "door-open",
    description: "Open and push through standard doors. Trained with RL in Isaac Sim with sim-to-real transfer.",
    robots: ["G1", "H1"],
    brands: ["Unitree"],
    category: "humanoid",
    method: "RL (SAC)",
    episodes: 0,
    successRate: 0.82,
    price: 0,
    version: "v0.9.0",
    authorId: "community-dev",
    signed: false,
    downloads: 320,
    rating: 4.3,
    accent: "#34D399",
    tags: ["locomotion", "interaction", "free"],
    proof: [
      { suite: "Navigation", passRate: 0.85, runs: 200 },
      { suite: "Manipulation", passRate: 0.80, runs: 200 },
      { suite: "Edge cases", passRate: 0.72, runs: 100 },
    ],
  },
  {
    id: "sort-conveyor",
    name: "sort-conveyor",
    description: "Sort objects on a conveyor belt by color/shape. Real-time inference with diffusion policy.",
    robots: ["UR5e", "Franka Panda"],
    brands: ["Universal Robots", "Franka"],
    category: "arm",
    method: "Diffusion Policy",
    episodes: 1600,
    successRate: 0.91,
    price: 69,
    version: "v1.2.0",
    authorId: "factory-bot",
    signed: true,
    downloads: 670,
    rating: 4.8,
    accent: "#FBBF24",
    tags: ["sorting", "industrial", "real-time"],
    proof: [
      { suite: "Detection", passRate: 0.96, runs: 500 },
      { suite: "Grasping", passRate: 0.92, runs: 600 },
      { suite: "Sorting accuracy", passRate: 0.91, runs: 400 },
    ],
  },
];

export function getAuthor(id: string): Author | undefined {
  return AUTHORS.find((a) => a.id === id);
}

export function getSkill(id: string): SkillListing | undefined {
  return SKILLS.find((s) => s.id === id);
}

export const MARKETPLACE_STATS = {
  totalSkills: SKILLS.length,
  totalDownloads: SKILLS.reduce((s, k) => s + k.downloads, 0),
  totalAuthors: AUTHORS.length,
  takeRate: 0.15,
};
