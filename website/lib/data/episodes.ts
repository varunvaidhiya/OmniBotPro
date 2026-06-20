/*
 * Dataset episodes for OhhO Data — generated for the selected robot.
 *
 * Was a fixed 9-DOF mobile-manipulation catalog; now `episodesFor(config)`
 * sizes the state timeseries to the robot's DOF (arm = armDof, base = baseDof),
 * names the cameras from the robot's sensors, and picks task instructions that
 * fit the robot (manipulation for arms, flight for drones, patrol for legged
 * bases, navigation for wheeled bases). The export schema follows too.
 */

import { useState, useCallback, useEffect } from "react";

import type { RobotConfig } from "@/lib/garage/robot-config";

export type EpisodeStatus = "kept" | "discard" | "review";

export interface Episode {
  id: string;
  frames: number;
  durationSec: number;
  status: EpisodeStatus;
  instruction: string;
  cameras: string[];
  /** State timeseries, length = frames. arm is [frames][armDof], base is [frames][baseDof]. */
  states: {
    arm: number[][];
    base: number[][];
  };
  keyFrames: number[];
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function generateStateSequence(
  frames: number,
  dim: number,
  smoothness = 0.05,
  range: [number, number] = [0.2, 0.8],
): number[][] {
  const result: number[][] = [];
  if (dim <= 0) {
    for (let i = 0; i < frames; i++) result.push([]);
    return result;
  }
  const current = new Array(dim).fill(0).map(() => range[0] + Math.random() * (range[1] - range[0]));
  for (let i = 0; i < frames; i++) {
    result.push([...current]);
    for (let d = 0; d < dim; d++) {
      current[d] += (Math.random() - 0.5) * smoothness;
      current[d] = Math.max(range[0], Math.min(range[1], current[d]));
    }
  }
  return result;
}

/** Short camera tokens (e.g. "front", "wrist") from the robot's camera sensors. */
function cameraTokens(config: RobotConfig): string[] {
  const cams = config.sensors.filter((s) =>
    ["rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera", "bev"].includes(s.kind),
  );
  const toks = cams.map((c) => c.label.toLowerCase().replace(/\s*camera\s*/i, "").replace(/\s+/g, "_").trim() || "cam");
  return toks.length ? toks : ["cam"];
}

/** Task instructions appropriate to the robot's capabilities. */
function instructionsFor(config: RobotConfig): string[] {
  const cap = config.capabilities;
  if (cap.isAerial) {
    return [
      "Survey the north field and return to home.",
      "Inspect the cell tower at 30 m altitude.",
      "Follow the perimeter waypoints and map the area.",
      "Orbit the structure and capture footage.",
      "Track the moving ground vehicle.",
      "Climb to 50 m and hold position in wind.",
      "Return to launch on low battery. (Aborted)",
      "Scan the rooftop for thermal anomalies.",
    ];
  }
  if (cap.isUnderwater) {
    return [
      "Inspect the dam wall for cracks.",
      "Follow the pipeline and log anomalies.",
      "Hold depth at 20 m against the current.",
      "Survey the hull and return to tether.",
      "Grasp the sample and surface. (Lost grip)",
    ];
  }
  if (cap.canManipulate && cap.canNavigate) {
    return [
      "Pick up the blue cube and place it in the bin.",
      "Navigate to the lab bench and grasp the beaker.",
      "Pick up the red apple. (Failed to grasp)",
      "Sort the objects into their corresponding trays.",
      "Open the drawer and retrieve the screwdriver.",
      "Close the door. (Collision detected)",
      "Pick up the sponge and wipe the table.",
      "Hand the bottle to the human.",
      "Stack the three blocks on top of each other.",
      "Push the chair under the desk.",
    ];
  }
  if (cap.canManipulate) {
    return [
      "Pick the part from the feeder and place on the jig.",
      "Insert the peg into the hole.",
      "Screw the cap onto the bottle.",
      "Palletize the box onto the stack.",
      "Hand off the component to the conveyor.",
      "Grasp the fragile vial. (Slipped)",
      "Trace the weld seam along the panel.",
    ];
  }
  if (cap.isLegged) {
    return [
      "Patrol the corridor and return to dock.",
      "Climb the stairs to level 2.",
      "Inspect the gauges in the plant room.",
      "Traverse the gravel to the waypoint.",
      "Recover from a slip on the ramp. (Fell)",
      "Follow the operator at walking pace.",
    ];
  }
  // wheeled / tracked / delivery navigation
  return [
    "Navigate to the loading dock.",
    "Deliver the parcel to room 204.",
    "Follow the lane to the charging station.",
    "Avoid the cones and reach the target.",
    "Map the warehouse aisle.",
    "Return home. (Path blocked)",
  ];
}

function buildEpisode(
  id: string,
  frames: number,
  status: EpisodeStatus,
  instruction: string,
  config: RobotConfig,
): Episode {
  return {
    id,
    frames,
    durationSec: frames / 30,
    status,
    instruction,
    cameras: cameraTokens(config),
    states: {
      arm: generateStateSequence(frames, config.armDof, 0.08, [0.1, 0.9]),
      base: generateStateSequence(frames, config.baseDof, 0.03, [0.4, 0.6]),
    },
    keyFrames: [Math.floor(frames * 0.2), Math.floor(frames * 0.5), Math.floor(frames * 0.8)],
  };
}

// ── Catalog ───────────────────────────────────────────────────────────────────

const FRAMES = [412, 388, 97, 455, 520, 210, 390, 345, 420, 380, 310, 480];
const STATUSES: EpisodeStatus[] = ["kept", "kept", "discard", "review", "kept", "discard", "kept", "kept", "review", "kept", "kept", "kept"];

/** Build the full episode catalog for a robot. */
export function episodesFor(config: RobotConfig): Episode[] {
  const instructions = instructionsFor(config);
  return FRAMES.map((frames, i) => {
    const instr = instructions[i % instructions.length];
    // Episodes whose instruction notes a failure are discards.
    const failed = /\(.*(fail|abort|collision|blocked|slip|fell|lost|slipped)/i.test(instr);
    const status: EpisodeStatus = failed ? "discard" : STATUSES[i];
    return buildEpisode(`ep_${(148 + i).toString().padStart(4, "0")}`, frames, status, instr, config);
  });
}

export function useEpisodeCatalog(config: RobotConfig) {
  const [episodes, setEpisodes] = useState<Episode[]>(() => episodesFor(config));

  useEffect(() => {
    setEpisodes(episodesFor(config));
  }, [config]);

  const setStatus = useCallback((episodeId: string, status: EpisodeStatus) => {
    setEpisodes((prev) => prev.map((ep) => (ep.id === episodeId ? { ...ep, status } : ep)));
  }, []);

  return { episodes, setStatus };
}

function asciiLabel(l: string): string {
  return l.replace(/ω/g, "omega").replace(/δ/g, "delta").replace(/[^\w]/g, "_") || "axis";
}

export function exportParquet(episode: Episode, config: RobotConfig): void {
  if (typeof window === "undefined") return;
  const armCols = config.joints.map((j) => j.name);
  const baseCols = config.baseActionLabels.map((l) => `base_${asciiLabel(l)}`);
  const header = [
    "# OhhO Data — LeRobot-format dataset export",
    `# Robot: ${config.name}`,
    `# Episode: ${episode.id}`,
    `# Instruction: ${episode.instruction}`,
    `# Frames: ${episode.frames}, Duration: ${episode.durationSec.toFixed(1)}s`,
    `# Status: ${episode.status}`,
    `# Cameras: ${episode.cameras.join(", ")}`,
    "",
    `# Schema: ${config.totalDof}-DOF ${config.category} (${config.armDof} arm + ${config.baseDof} base)`,
    `# Columns: frame,${[...armCols, ...baseCols].join(",")}`,
    "",
  ];

  const rows: string[] = [];
  for (let f = 0; f < episode.frames; f++) {
    const arm = episode.states.arm[f] ?? episode.states.arm[episode.states.arm.length - 1] ?? [];
    const base = episode.states.base[f] ?? episode.states.base[episode.states.base.length - 1] ?? [];
    rows.push([f, ...arm.map((v) => v.toFixed(4)), ...base.map((v) => v.toFixed(4))].join(","));
  }

  const content = [...header, ...rows].join("\n");
  const blob = new Blob([content], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `${episode.id}_export.csv`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
