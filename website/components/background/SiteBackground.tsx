"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { usePathname } from "next/navigation";

import {
  BACKGROUND_PRESENTATION,
  BACKGROUND_REEL,
  CROSSFADE_SECONDS,
  MOTION_PREF_KEY,
  PLAYBACK_RATE,
  backgroundModeFor,
  detectPerfTier,
  type PerfTier,
} from "@/lib/background";
import BackgroundMotionToggle from "./BackgroundMotionToggle";

/*
 * SiteBackground — the fixed, full-viewport cinematic layer behind every page.
 *
 * Two <video> elements leapfrog each other: one plays while the other holds the
 * next clip, and they cross-fade so the reel never cuts. Everything above the
 * footage (grade, scrim, vignette, grain, grid) lives in globals.css and is
 * driven by the CSS custom properties set on the root node here, so the
 * intensity of the whole stack is one number per route.
 *
 * The layer is position:fixed / pointer-events:none / aria-hidden — it never
 * intercepts a click, never scrolls, and is invisible to assistive tech.
 *
 * Motion is dropped entirely (still poster only) when any of these hold:
 *   - the route is operational or long-form reading (see backgroundModeFor)
 *   - the visitor asked for reduced motion, or turned the reel off themselves
 *   - the connection reports Save-Data or 2g/3g
 *   - autoplay was refused by the browser
 */

/** Viewport width below which the 640x360 renditions are used instead. */
const SMALL_VIEWPORT = 820;

type Capability = {
  /** Motion is permitted by the environment (reduced-motion, data saver…). */
  allowed: boolean;
  /** Serve the lighter -sm renditions. */
  small: boolean;
};

function readCapability(): Capability {
  if (typeof window === "undefined") return { allowed: false, small: false };

  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const small = window.innerWidth < SMALL_VIEWPORT;

  // navigator.connection is Chromium-only; absence just means "no signal".
  const conn = (navigator as Navigator & { connection?: { saveData?: boolean; effectiveType?: string } })
    .connection;
  const thrifty = Boolean(
    conn?.saveData || (conn?.effectiveType && /(^|-)(2g|3g)$/.test(conn.effectiveType)),
  );

  return { allowed: !reduced && !thrifty, small };
}

function readMotionPreference(): boolean {
  if (typeof window === "undefined") return true;
  try {
    return window.localStorage.getItem(MOTION_PREF_KEY) !== "off";
  } catch {
    return true; // private mode / storage blocked — default to the designed experience
  }
}

export default function SiteBackground() {
  const pathname = usePathname();
  const mode = backgroundModeFor(pathname);
  const preset = BACKGROUND_PRESENTATION[mode];

  // Everything below starts in its "no motion" state so the server render and
  // the first client render agree; capability is resolved in an effect.
  const [capability, setCapability] = useState<Capability>({ allowed: false, small: false });
  const [prefersMotion, setPrefersMotion] = useState(true);
  /*
   * Autoplay refused (Safari Low Power Mode, iOS data saver, a backgrounded
   * tab…). The clips stay mounted and the still frame stays on top; we retry
   * on the visitor's first gesture, which is the point browsers start allowing
   * playback. Distinct from `loadFailed`, which is fatal.
   */
  const [autoplayBlocked, setAutoplayBlocked] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);

  const [slots, setSlots] = useState<[number, number]>([0, 1]);
  const [active, setActive] = useState<0 | 1>(0);
  /** The idle slot only gets a source once the first clip is actually playing. */
  const [warm, setWarm] = useState(false);
  const [playing, setPlaying] = useState(false);

  const videoRefs = [
    useRef<HTMLVideoElement>(null),
    useRef<HTMLVideoElement>(null),
  ] as const;
  /* Guards the hand-off so a burst of timeupdate events can't start two. */
  const swappingRef = useRef(false);

  // Note: a blocked autoplay does NOT clear motionOn — the elements stay mounted
  // so the gesture retry below has something to start.
  const motionOn = preset.motion && capability.allowed && prefersMotion && !loadFailed;

  /* ── device tier → data-perf on <html> ───────────────────────────────────
   * Written to the root element rather than held in state, because the rules it
   * gates (globals.css, ":root[data-perf=lite]") apply to the whole document,
   * not just this subtree — the console panels that set backdrop-filter inline
   * live nowhere near here.
   */
  useEffect(() => {
    const root = document.documentElement;
    const setTier = () => {
      const tier: PerfTier = detectPerfTier();
      root.dataset.perf = tier;
    };
    setTier();

    // A coarse pointer can appear or disappear (tablet keyboard, hybrid laptop).
    const pointerQuery = window.matchMedia("(pointer: coarse)");
    pointerQuery.addEventListener("change", setTier);
    return () => pointerQuery.removeEventListener("change", setTier);
  }, []);

  /*
   * Pause decorative animation once it scrolls out of view.
   *
   * Done centrally with one observer rather than by touching each component,
   * because these classes are plain decorative divs scattered across the hero
   * and ten console pages — there is no shared component to hook into, and
   * spreading this logic over eleven files would guarantee the next one forgets.
   *
   * Purely an optimisation: an element that is off-screen is not being looked
   * at, so stopping its animation changes nothing the visitor can see. It only
   * lets the compositor stop producing frames. Re-scanned per route.
   */
  useEffect(() => {
    const SELECTOR =
      ".hero-grid, .hero-orb-1, .hero-orb-2, .hero-orb-3, .scroll-hint, .badge-dot";

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          (entry.target as HTMLElement).dataset.offscreen = entry.isIntersecting
            ? "false"
            : "true";
        }
      },
      // A little margin so nothing is caught mid-resume at the edge.
      { rootMargin: "120px" },
    );

    const scan = () => {
      document.querySelectorAll(SELECTOR).forEach((el) => observer.observe(el));
    };
    scan();

    // Client-side navigation swaps the subtree under us after this effect runs.
    const mutations = new MutationObserver(scan);
    mutations.observe(document.body, { childList: true, subtree: true });

    return () => {
      observer.disconnect();
      mutations.disconnect();
    };
  }, [pathname]);

  /* ── pause ambient animation while the tab is hidden ─────────────────── */
  useEffect(() => {
    const root = document.documentElement;
    const sync = () => {
      root.dataset.hidden = document.hidden ? "true" : "false";
    };
    sync();
    document.addEventListener("visibilitychange", sync);
    return () => {
      document.removeEventListener("visibilitychange", sync);
      delete root.dataset.hidden;
    };
  }, []);

  /* ── capability + stored preference ──────────────────────────────────── */
  useEffect(() => {
    setCapability(readCapability());
    setPrefersMotion(readMotionPreference());

    const reduceQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    const onChange = () => setCapability(readCapability());
    reduceQuery.addEventListener("change", onChange);
    window.addEventListener("resize", onChange);

    // Other tabs (or the toggle below) can flip the preference.
    const onStorage = (e: StorageEvent) => {
      if (e.key === MOTION_PREF_KEY) setPrefersMotion(readMotionPreference());
    };
    const onLocal = () => setPrefersMotion(readMotionPreference());
    window.addEventListener("storage", onStorage);
    window.addEventListener("ohho:bg-motion-change", onLocal);

    return () => {
      reduceQuery.removeEventListener("change", onChange);
      window.removeEventListener("resize", onChange);
      window.removeEventListener("storage", onStorage);
      window.removeEventListener("ohho:bg-motion-change", onLocal);
    };
  }, []);

  /*
   * The small rendition is used for two different reasons that happen to want
   * the same file: a narrow viewport does not need 1280x720, and the modes that
   * used to ask for a blur() get their softening from upscaling a 640x360 plate
   * instead of from a per-frame convolution. See lib/background.ts.
   */
  const srcFor = useCallback(
    (index: number) => {
      const light = capability.small || preset.soften;
      return `/videos/background/${BACKGROUND_REEL[index].id}${light ? "-sm" : ""}.mp4`;
    },
    [capability.small, preset.soften],
  );

  /* ── keep each <video> pointed at its slot's clip ────────────────────── */
  useEffect(() => {
    if (!motionOn) return;
    videoRefs.forEach((ref, i) => {
      const video = ref.current;
      if (!video) return;
      // Slot 1 stays empty until the first clip is playing, so the initial page
      // load isn't spending bandwidth on footage nobody can see yet.
      if (i !== active && !warm) return;

      const want = srcFor(slots[i]);
      if (video.getAttribute("src") !== want) {
        video.setAttribute("src", want);
        video.load();
      }
      video.playbackRate = PLAYBACK_RATE;
    });
    // videoRefs is a stable tuple of refs
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slots, motionOn, warm, srcFor, active]);

  /* ── drive playback of the active slot ───────────────────────────────── */
  useEffect(() => {
    if (!motionOn) {
      videoRefs.forEach((ref) => ref.current?.pause());
      setPlaying(false);
      return;
    }
    const video = videoRefs[active].current;
    if (!video) return;
    video.playbackRate = PLAYBACK_RATE;
    void video
      .play()
      .then(() => setAutoplayBlocked(false))
      .catch(() => setAutoplayBlocked(true));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [motionOn, active]);

  /* ── retry a refused autoplay on the first user gesture ──────────────── */
  useEffect(() => {
    if (!motionOn || !autoplayBlocked) return;

    const retry = () => {
      const video = videoRefs[active].current;
      if (!video) return;
      video.playbackRate = PLAYBACK_RATE;
      void video
        .play()
        .then(() => setAutoplayBlocked(false))
        .catch(() => {
          /* still refused — the next gesture gets another go */
        });
    };

    const events = ["pointerdown", "keydown", "touchstart", "scroll"] as const;
    events.forEach((e) =>
      window.addEventListener(e, retry, { passive: true }),
    );
    return () => events.forEach((e) => window.removeEventListener(e, retry));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [motionOn, autoplayBlocked, active]);

  /* ── pause while the tab is hidden ───────────────────────────────────── */
  useEffect(() => {
    if (!motionOn) return;
    const onVisibility = () => {
      const video = videoRefs[active].current;
      if (!video) return;
      if (document.hidden) video.pause();
      else void video.play().catch(() => {});
    };
    document.addEventListener("visibilitychange", onVisibility);
    return () => document.removeEventListener("visibilitychange", onVisibility);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [motionOn, active]);

  /*
   * Hand over to the other slot once the playing clip is CROSSFADE_SECONDS from
   * its end. Remaining time is divided by the playback rate because the clip is
   * running in slow motion — media seconds are not wall-clock seconds.
   */
  const handleTimeUpdate = useCallback(
    (slot: 0 | 1) => () => {
      if (slot !== active || swappingRef.current) return;
      const video = videoRefs[slot].current;
      if (!video || !Number.isFinite(video.duration) || video.duration === 0) return;

      const wallRemaining = (video.duration - video.currentTime) / PLAYBACK_RATE;
      if (wallRemaining > CROSSFADE_SECONDS) return;

      const next = (1 - slot) as 0 | 1;
      const incoming = videoRefs[next].current;
      if (!incoming || !incoming.getAttribute("src")) return;

      swappingRef.current = true;
      incoming.currentTime = 0;
      incoming.playbackRate = PLAYBACK_RATE;
      void incoming.play().catch(() => setAutoplayBlocked(true));
      setActive(next);

      // Once the dissolve has finished, park the outgoing clip and queue up the
      // one after next in the slot that just freed up.
      window.setTimeout(() => {
        const outgoing = videoRefs[slot].current;
        outgoing?.pause();
        setSlots((prev) => {
          const updated: [number, number] = [...prev] as [number, number];
          updated[slot] = (prev[next] + 1) % BACKGROUND_REEL.length;
          return updated;
        });
        swappingRef.current = false;
      }, CROSSFADE_SECONDS * 1000);
    },
    // videoRefs is a stable tuple of refs
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [active],
  );

  const handlePlaying = useCallback(
    (slot: 0 | 1) => () => {
      if (slot !== active) return;
      setPlaying(true);
      setWarm(true); // safe to start fetching the next clip now
    },
    [active],
  );

  /*
   * The still frame sits under the videos: it is what shows before the first
   * clip decodes, and it is the entire background in `still` mode. `lab-bokeh`
   * is the most abstract plate in the reel, which is what a console or a docs
   * page wants behind it.
   */
  const stillClip = mode === "still" ? "lab-bokeh" : BACKGROUND_REEL[0].id;

  /*
   * Intensity is written straight to the DOM rather than through React state,
   * so a scroll never triggers a re-render of a layer containing two <video>
   * elements.
   *
   * On the landing page the reel starts cinematic and recedes to `ambient` over
   * the first viewport of scrolling. That is the whole "present but never in the
   * way" idea in one gesture: the footage carries the hero, then steps back as
   * soon as the visitor is actually reading. It is also what keeps mid-page copy
   * comfortably above WCAG AA, which the full-strength grade does not.
   */
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;

    /*
     * Only two properties are written now, and both are free: layer opacity is
     * a compositor operation, not a repaint. The old version also interpolated
     * --bg-brightness and --bg-blur, which fed a `filter` chain that had a
     * 0.3s transition on it — so every scroll frame kicked off a brand-new blur
     * interpolation that the next frame immediately replaced. The filter was
     * being recomputed continuously and never once reached its target value.
     */
    const apply = (p: number) => {
      const from = BACKGROUND_PRESENTATION[mode];
      const to = BACKGROUND_PRESENTATION.ambient;
      const mix = (a: number, b: number) => a + (b - a) * p;
      root.style.setProperty("--bg-video-opacity", `${mix(from.videoOpacity, to.videoOpacity)}`);
      root.style.setProperty("--bg-scrim", `${mix(from.scrim, to.scrim)}`);
    };

    // Only the landing page recedes; everywhere else the preset is fixed.
    if (mode !== "cinematic") {
      apply(0);
      return;
    }

    const progress = () => {
      const span = Math.max(window.innerHeight * 0.9, 1);
      return Math.min(Math.max(window.scrollY / span, 0), 1);
    };

    let ticking = false;
    const onScroll = () => {
      if (ticking) return;
      ticking = true;
      window.requestAnimationFrame(() => {
        apply(progress());
        ticking = false;
      });
    };

    // Applied directly, not through the throttle: the opening state must not
    // wait on a frame callback (a restored scroll position needs it immediately).
    apply(progress());
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });
    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
    };
  }, [mode]);

  return (
    <>
      <div
        ref={rootRef}
        className="site-bg"
        aria-hidden="true"
        data-mode={mode}
        /*
         * Only the static value lives here. The four intensity properties are
         * owned by the scroll effect above — putting them in the inline style
         * too would let a re-render stamp over the scrolled value.
         * app/globals.css carries cinematic defaults for the first paint.
         */
        style={{ "--bg-crossfade": `${CROSSFADE_SECONDS}s` } as React.CSSProperties}
      >
        {/*
         * The reel wrapper carries the grade + opacity for the whole route, so
         * the elements inside are free to use opacity purely for the dissolve.
         */}
        <div className="site-bg-reel">
          {/* still frame — what shows before the first clip decodes */}
          <div
            className="site-bg-still"
            style={{
              backgroundImage: `url(/videos/background/${stillClip}.jpg)`,
              opacity: playing ? 0 : 1,
            }}
          />

          {motionOn &&
            ([0, 1] as const).map((slot) => (
              <video
                key={slot}
                ref={videoRefs[slot]}
                className="site-bg-video"
                style={{ opacity: active === slot ? 1 : 0 }}
                muted
                playsInline
                // No `loop`: each clip hands off to the other slot instead.
                preload={slot === active ? "auto" : "metadata"}
                poster={`/videos/background/${BACKGROUND_REEL[slots[slot]].id}.jpg`}
                onTimeUpdate={handleTimeUpdate(slot)}
                onPlaying={handlePlaying(slot)}
                // A genuine media error (missing/undecodable file) is fatal —
                // drop to the still frame rather than retrying forever.
                onError={() => setLoadFailed(true)}
                tabIndex={-1}
              />
            ))}
        </div>

        {/* grade → scrim → vignette → grain → grid: the legibility stack */}
        <div className="site-bg-grade" />
        <div className="site-bg-scrim" />
        <div className="site-bg-vignette" />
        <div className="site-bg-grain" />
        <div className="site-bg-grid" />
      </div>

      {/*
       * Offer the control only when there is something to act on: the reel is
       * running (so it can be stopped), or the visitor stopped it themselves
       * (so they can start it again).
       */}
      {preset.motion && capability.allowed && !loadFailed && (playing || !prefersMotion) && (
        <BackgroundMotionToggle on={prefersMotion} onChange={setPrefersMotion} />
      )}
    </>
  );
}
