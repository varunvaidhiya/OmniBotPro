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
 * PERFORMANCE NOTE — read before adding anything here.
 *
 * This layer is `position: fixed` and full-viewport, and the page never stops
 * painting (there are always-running ambient animations above it). That means
 * anything expensive here is paid on *every frame, forever*, not once. Two CSS
 * features are therefore banned from this stack:
 *
 *   - `filter: blur()`  — a real convolution over the whole viewport, redone
 *     every frame. Measured at ~2x the total frame cost of the landing page.
 *   - `mix-blend-mode`  — forces the compositor to read the backdrop back out
 *     of the GPU before it can blend. Measured at ~1.4x.
 *
 * Together they also made every `backdrop-filter` glass tile far more expensive
 * than it needed to be, because each tile had to re-sample a backdrop that was
 * itself being blurred and blended. Removing both took the landing page from
 * ~10 fps to ~25 fps before any other change.
 *
 * What replaced them:
 *
 * `videoOpacity` now carries the old `brightness()` as well. Compositing the
 * footage at opacity a over the near-black page colour is arithmetically almost
 * the same as brightness(b) at opacity a — `result = bg(1-a) + video*b*a` versus
 * `bg(1-ab) + video*ab` — and since `--bg` is #0A0E1A the residual difference is
 * under 5/255 in the shadows. Opacity is a free compositor operation; a filter
 * is not. So each value below is the old videoOpacity x the old brightness.
 *
 * `soften` replaces the old `blurPx`. Instead of convolving the frame every
 * tick, the modes that wanted a soft plate simply load the 640x360 rendition
 * and let the GPU's bilinear upscale do it. Scaling a small texture up *is* a
 * blur, and it costs nothing — it also downloads ~3x less video. `cinematic`
 * asked for only 1.5px, which is imperceptible, so it keeps the sharp plate.
 *
 * `scrim` still weights the gradient bands where copy actually sits — under the
 * fixed nav, and along the bottom edge.
 */
export const BACKGROUND_PRESENTATION: Record<
  BackgroundMode,
  {
    /** Old videoOpacity x old brightness — see note above. */
    videoOpacity: number;
    scrim: number;
    /** Use the 640x360 rendition, upscaled, in place of a blur(). */
    soften: boolean;
    motion: boolean;
  }
> = {
  cinematic: { videoOpacity: 0.46, scrim: 0.6, soften: false, motion: true },
  ambient: { videoOpacity: 0.26, scrim: 0.78, soften: true, motion: true },
  still: { videoOpacity: 0.13, scrim: 0.9, soften: true, motion: false },
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

/* ── device capability tier ──────────────────────────────────────────────
 *
 * `full` gets the designed treatment. `lite` drops the effects whose cost is
 * paid per-pixel-per-frame — chiefly `backdrop-filter`, which the marketing
 * pages use on ~50 glass tiles covering roughly 4.4x the viewport area.
 *
 * On a desktop GPU that is affordable. On a tablet it is not: the same tiles at
 * 2x device-pixel-ratio mean the compositor re-samples and re-blurs ~25
 * megapixels of backdrop every frame, and it also has to keep a snapshot
 * texture per tile resident. That combination is what makes the page stutter
 * and then stall on an iPad — it runs out of both fill rate and layer memory.
 *
 * Touch is the signal, not screen size: a coarse pointer means a phone or a
 * tablet, which means a mobile GPU and a battery. Core count and device memory
 * catch low-end laptops too, where they're exposed (Safari reports neither, so
 * the pointer check is what carries iPadOS).
 *
 * `lite` keeps the video reel. Video decode is hardware-accelerated and cheap;
 * it was never the problem. What it drops is the compositing work stacked on
 * top of it.
 */
export type PerfTier = "full" | "lite";

export function detectPerfTier(): PerfTier {
  if (typeof window === "undefined") return "full";

  const coarse = window.matchMedia("(pointer: coarse)").matches;

  // Chromium-only; `undefined` on Safari/Firefox just means "no signal", so
  // default high and let the pointer check decide.
  const cores = navigator.hardwareConcurrency ?? 8;
  const memory = (navigator as Navigator & { deviceMemory?: number }).deviceMemory ?? 8;

  return coarse || cores <= 4 || memory <= 4 ? "lite" : "full";
}
