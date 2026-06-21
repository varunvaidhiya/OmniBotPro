/*
 * OhhO Twin — pure data types and constants (no React hooks).
 *
 * This file is safe to import from server-side code (API routes, MCP tools).
 * The simulation hook (useTwinSimulation) lives in twin-hooks.ts and imports
 * this file + React.
 */

export interface TwinState {
  x: number;
  y: number;
  theta: number;
  vx: number;
  vy: number;
  vtheta: number;
  /** Motor temperatures in °C (per-joint). */
  motorTemps: number[];
  /** Battery fraction 0..1. */
  battery: number;
  /** Timestamp (ms epoch). */
  t: number;
}

export interface Prediction {
  label: string;
  current: string;
  predicted: string;
  trend: "up" | "down" | "stable";
  urgency: "low" | "medium" | "high";
  /** Hours until predicted threshold crossing. */
  hoursToThreshold: number;
}

export interface WhatIfResult {
  scenario: string;
  change: string;
  outcome: string;
  successRate: number;
  /** Compared to the original run. */
  delta: number;
}

export const PREDICTIONS: Prediction[] = [
  {
    label: "Motor temp (left knee)",
    current: "62°C",
    predicted: "78°C",
    trend: "up",
    urgency: "medium",
    hoursToThreshold: 4.2,
  },
  {
    label: "Battery",
    current: "82%",
    predicted: "61%",
    trend: "down",
    urgency: "low",
    hoursToThreshold: 2.8,
  },
  {
    label: "Joint wear (wrist)",
    current: "MTBF 412h",
    predicted: "MTBF 380h",
    trend: "down",
    urgency: "medium",
    hoursToThreshold: 72,
  },
];

export const WHATIF_RESULTS: WhatIfResult[] = [
  {
    scenario: "Different grasp angle",
    change: "+15° approach",
    outcome: "Pick succeeds with better clearance",
    successRate: 0.91,
    delta: 0.17,
  },
  {
    scenario: "Reduced speed",
    change: "0.15 m/s (was 0.3)",
    outcome: "Smoother trajectory, same success",
    successRate: 0.88,
    delta: 0.14,
  },
];

export const TWIN_STATS = {
  simWorld: "Isaac Sim",
  matchScore: 0.97,
  driftCm: 3,
  replayStorageMb: 2.1,
};
