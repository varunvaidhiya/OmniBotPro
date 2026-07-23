/*
 * The site-wide cinematic background reel.
 *
 * Seven robotics-lab plates play as one continuous, slow, cross-fading loop
 * behind every page (components/background/SiteBackground). They are graded and
 * scrimmed in CSS so foreground copy always wins — see the "SITE VIDEO
 * BACKGROUND" block in app/globals.css.
 *
 * Assets live in public/videos/background/ and are produced from the raw
 * captures in assets/background/. Each clip ships three renditions:
 *   <id>.mp4      1280x720, silent, ~800 KB   — desktop
 *   <id>-sm.mp4   640x360,  silent, ~250 KB   — phones / metered connections
 *   <id>.jpg      1280x720, ~50 KB            — poster + reduced-motion still
 */

export type BackgroundClip = {
  /** Basename of the three renditions in public/videos/background/. */
  id: string;
  /** What the plate shows — documentation only, the layer is aria-hidden. */
  label: string;
};

/*
 * Order is deliberate: the reel opens on the wheeled manipulator (the plate
 * closest to OmniBot itself), drops into the abstract bokeh plate to let the
 * hero copy breathe, and only then moves through the busier lab scenes.
 */
export const BACKGROUND_REEL: BackgroundClip[] = [
  { id: "manipulator", label: "Wheeled manipulator on a lab floor" },
  { id: "lab-bokeh", label: "Defocused lab bench light trails" },
  { id: "quadruped", label: "Quadruped robot at rest" },
  { id: "lab-wide", label: "Wide robotics lab, arm and quadruped" },
  { id: "neural", label: "Graph network on a workstation display" },
  { id: "cobot", label: "Collaborative arm on a work cell" },
  { id: "drone", label: "Drone on a bench beside a rover" },
];

/**
 * How present the reel is on a given route.
 *
 * - `cinematic` — the marketing landing page. The reel is the hero surface.
 * - `ambient`   — other marketing//content pages. Present but pulled well back
 *                 so long-form copy stays comfortable.
 * - `still`     — operational and reading surfaces (console, garage, account,
 *                 auth, docs). No motion at all: a near-black still frame only,
 *                 because moving footage behind live robot telemetry or a
 *                 sign-in form is a genuine distraction, not a flourish.
 */
export type BackgroundMode = "cinematic" | "ambient" | "still";

/** Route prefixes that must never show moving footage. */
const STILL_ROUTES = [
  "/console",
  "/garage",
  "/account",
  "/login",
  "/auth",
  "/upgrade",
  "/docs",
];

export function backgroundModeFor(pathname: string | null): BackgroundMode {
  if (!pathname) return "ambient";
  if (STILL_ROUTES.some((r) => pathname === r || pathname.startsWith(`${r}/`))) {
    return "still";
  }
  return pathname === "/" ? "cinematic" : "ambient";
}

/**
 * Per-mode presentation of the reel.
 *
 * `brightness` does the heavy lifting rather than a flat dark overlay: scaling
 * luminance multiplicatively keeps the plates' blacks black and only pulls down
 * the highlights (screens, LED strips) that would otherwise fight white text. A
 * flat scrim would instead lift the blacks and leave the footage looking milky.
 * `scrim` therefore only has to weight the gradient bands where copy actually
 * sits — under the fixed nav, and along the bottom edge.
 *
 * `blurPx` pushes the footage optically into the background plane, so foreground
 * text reads as a separate, sharper layer.
 */
export const BACKGROUND_PRESENTATION: Record<
  BackgroundMode,
  {
    videoOpacity: number;
    brightness: number;
    scrim: number;
    blurPx: number;
    motion: boolean;
  }
> = {
  cinematic: { videoOpacity: 0.92, brightness: 0.5, scrim: 0.6, blurPx: 1.5, motion: true },
  ambient: { videoOpacity: 0.72, brightness: 0.36, scrim: 0.78, blurPx: 3, motion: true },
  still: { videoOpacity: 0.5, brightness: 0.26, scrim: 0.9, blurPx: 6, motion: false },
};

/** Seconds of cross-fade between two clips. Long enough to read as a dissolve. */
export const CROSSFADE_SECONDS = 2.2;

/**
 * Playback rate for every clip. The source plates are 8 s of fairly brisk camera
 * movement; at 0.62x each one runs ~13 s and the motion stops pulling the eye
 * away from the copy.
 */
export const PLAYBACK_RATE = 0.62;

/** localStorage key for the visitor's "stop the motion" preference. */
export const MOTION_PREF_KEY = "ohho:bg-motion";
