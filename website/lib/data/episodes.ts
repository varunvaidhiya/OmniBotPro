/*
 * Dataset episodes mock catalog for OhhO Data.
 *
 * Simulates a LeRobot-format dataset containing 9-DOF mobile-manipulation
 * episodes, complete with multi-camera views, statuses, and time-series data
 * generators for the state plots.
 */

export type EpisodeStatus = "kept" | "discard" | "review";

export interface Episode {
  id: string;
  frames: number;
  durationSec: number;
  status: EpisodeStatus;
  instruction: string;
  cameras: string[];
  /** Mock 9-DOF state timeseries arrays, length = frames */
  states: {
    arm: number[][]; // [frames][6]
    base: number[][]; // [frames][3]
  };
  keyFrames: number[]; // Frame indices where significant actions occur
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function generateStateSequence(
  frames: number,
  dim: number,
  smoothness: number = 0.05,
  range: [number, number] = [0.2, 0.8]
): number[][] {
  const result: number[][] = [];
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

function createEpisode(id: string, frames: number, status: EpisodeStatus, instruction: string): Episode {
  return {
    id,
    frames,
    durationSec: frames / 30, // assuming 30 fps
    status,
    instruction,
    cameras: ["wrist", "front", "BEV"],
    states: {
      arm: generateStateSequence(frames, 6, 0.08, [0.1, 0.9]),
      base: generateStateSequence(frames, 3, 0.03, [0.4, 0.6]),
    },
    keyFrames: [Math.floor(frames * 0.2), Math.floor(frames * 0.5), Math.floor(frames * 0.8)],
  };
}

// ── Catalog ───────────────────────────────────────────────────────────────────

export const EPISODES: Episode[] = [
  createEpisode("ep_0148", 412, "kept", "Pick up the blue cube and place it in the bin."),
  createEpisode("ep_0149", 388, "kept", "Navigate to the lab bench and grasp the beaker."),
  createEpisode("ep_0150", 97, "discard", "Pick up the red apple. (Failed to grasp)"),
  createEpisode("ep_0151", 455, "review", "Sort the objects into their corresponding trays."),
  createEpisode("ep_0152", 520, "kept", "Open the drawer and retrieve the screwdriver."),
  createEpisode("ep_0153", 210, "discard", "Close the door. (Collision detected)"),
  createEpisode("ep_0154", 390, "kept", "Pick up the sponge and wipe the table."),
  createEpisode("ep_0155", 345, "kept", "Hand the bottle to the human."),
  createEpisode("ep_0156", 420, "review", "Stack the three blocks on top of each other."),
  createEpisode("ep_0157", 380, "kept", "Push the chair under the desk."),
  createEpisode("ep_0158", 310, "kept", "Press the big red button."),
  createEpisode("ep_0159", 480, "kept", "Navigate around the cones and reach the target."),
];

export function getEpisode(id: string): Episode {
  return EPISODES.find((e) => e.id === id) ?? EPISODES[0];
}
