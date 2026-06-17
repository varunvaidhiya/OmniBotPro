/*
 * Scene presets and simulation for OhhO Pilot camera view.
 *
 * Each scene defines a visual environment for the simulated camera feed — floor
 * grid, obstacles, and a primary "detected object" with a bounding box. The sim
 * drifts object positions and detection confidence each tick to give the camera
 * view a living feel.
 */

// ── Types ─────────────────────────────────────────────────────────────────────

export interface SceneObject {
  /** Unique label (e.g. "cup", "box", "pallet"). */
  label: string;
  /** Bounding box center, normalised [0,1] within the viewport. */
  x: number;
  y: number;
  /** Bounding box half-size, normalised. */
  hw: number;
  hh: number;
  /** Detection confidence [0,1]. */
  confidence: number;
  /** Display colour. */
  color: string;
}

export interface ScenePreset {
  id: string;
  name: string;
  /** Short blurb for the picker. */
  desc: string;
  /** CSS-like background description (used for rendering hints). */
  bgHint: string;
  /** Static obstacles on the ground plane (SVG rects). */
  obstacles: { x: number; y: number; w: number; h: number; color: string }[];
  /** Primary detected object. */
  primaryObject: SceneObject;
  /** Optional secondary objects. */
  secondaryObjects: SceneObject[];
}

// ── Colour tokens ─────────────────────────────────────────────────────────────

const AMBER = "#FBBF24";
const RED = "#F87171";
const GREEN = "#34D399";
const VIOLET = "#A78BFA";

// ── Scene presets ─────────────────────────────────────────────────────────────

export const SCENES: ScenePreset[] = [
  {
    id: "warehouse",
    name: "Warehouse",
    desc: "Shelves, pallets, and a target cup on the floor.",
    bgHint: "dark concrete floor, metal shelves",
    obstacles: [
      { x: 0.1, y: 0.65, w: 0.15, h: 0.25, color: "rgba(255,255,255,0.06)" },
      { x: 0.75, y: 0.55, w: 0.12, h: 0.35, color: "rgba(255,255,255,0.06)" },
    ],
    primaryObject: {
      label: "cup",
      x: 0.52,
      y: 0.58,
      hw: 0.06,
      hh: 0.08,
      confidence: 0.94,
      color: AMBER,
    },
    secondaryObjects: [
      { label: "box", x: 0.3, y: 0.7, hw: 0.08, hh: 0.06, confidence: 0.82, color: "rgba(255,255,255,0.3)" },
    ],
  },
  {
    id: "lab",
    name: "Lab bench",
    desc: "Workbench with tools and a target beaker.",
    bgHint: "white bench surface, lab equipment",
    obstacles: [
      { x: 0.05, y: 0.5, w: 0.9, h: 0.04, color: "rgba(255,255,255,0.08)" },
    ],
    primaryObject: {
      label: "beaker",
      x: 0.45,
      y: 0.55,
      hw: 0.05,
      hh: 0.1,
      confidence: 0.91,
      color: GREEN,
    },
    secondaryObjects: [
      { label: "tool", x: 0.65, y: 0.6, hw: 0.04, hh: 0.12, confidence: 0.77, color: VIOLET },
    ],
  },
  {
    id: "outdoor",
    name: "Outdoor path",
    desc: "Sidewalk with cones and a delivery target.",
    bgHint: "concrete path, grass edges",
    obstacles: [
      { x: 0.2, y: 0.8, w: 0.05, h: 0.08, color: RED },
      { x: 0.7, y: 0.75, w: 0.05, h: 0.08, color: RED },
    ],
    primaryObject: {
      label: "package",
      x: 0.5,
      y: 0.62,
      hw: 0.09,
      hh: 0.07,
      confidence: 0.88,
      color: AMBER,
    },
    secondaryObjects: [],
  },
];

export function getScene(id: string): ScenePreset {
  return SCENES.find((s) => s.id === id) ?? SCENES[0];
}

// ── Scene simulation ──────────────────────────────────────────────────────────

export interface SceneState {
  /** Current primary object (mutated each tick). */
  primary: SceneObject;
  secondary: SceneObject[];
  tick: number;
}

/**
 * Create a mutable scene state from a preset. Call `tickScene` each frame to
 * animate object positions and confidence jitter.
 */
export function initSceneState(preset: ScenePreset): SceneState {
  return {
    primary: { ...preset.primaryObject },
    secondary: preset.secondaryObjects.map((o) => ({ ...o })),
    tick: 0,
  };
}

/** Advance the scene state one tick. Objects drift slightly, confidence jitters. */
export function tickScene(state: SceneState, preset: ScenePreset): SceneState {
  const t = state.tick + 1;
  const drift = 0.004; // max per-tick normalised drift

  const jitter = (base: number, seed: number) =>
    base + Math.sin(t * 0.07 + seed) * drift;

  const confJitter = (base: number, seed: number) =>
    Math.max(0.6, Math.min(0.99, base + Math.sin(t * 0.12 + seed) * 0.04));

  const primary: SceneObject = {
    ...state.primary,
    x: jitter(preset.primaryObject.x, 0),
    y: jitter(preset.primaryObject.y, 1.5),
    confidence: confJitter(preset.primaryObject.confidence, 2),
  };

  const secondary = state.secondary.map((o, i) => ({
    ...o,
    x: jitter(preset.secondaryObjects[i]?.x ?? o.x, 3 + i),
    y: jitter(preset.secondaryObjects[i]?.y ?? o.y, 4 + i),
    confidence: confJitter(preset.secondaryObjects[i]?.confidence ?? o.confidence, 5 + i),
  }));

  return { primary, secondary, tick: t };
}
