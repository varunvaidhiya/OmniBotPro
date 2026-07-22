# OhhO Twin — Higgsfield Video Prompt

- **Product:** OhhO Twin — *Your real robot. Mirrored in simulation. Live.*
- **Category:** Operations · **Primary accent:** Violet `#7C3AED`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** A live digital twin — real telemetry streams into a persistent sim; replay, scrub, what-if and predict, side by side with the physical robot.

## Full film — paste into Higgsfield storyboard mode (6 shots × 15 s ≈ 90 s)

Each `SHOT` below is one 15-second generation. Keep the STYLE and ACCENT paragraphs
at the top of **every** shot's prompt. In storyboard / long-video mode, let
Higgsfield carry continuity between shots; when generating shots one at a time,
feed each shot's **last frame** as the next shot's **first frame**.

```
STYLE — OhhO (identical for every film, do not change):
Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab / light-industrial space: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, tool walls, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm and 50mm prime lenses, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move per shot, no shake. Materials must read as physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections and fingerprint-level detail. Every monitor or tablet in frame shows only a soft, out-of-focus dark dashboard — never readable text. Mood: calm, confident, precise, quietly futuristic. Color grade: cool and high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated.
NEGATIVE: no CGI look, no 3-D render or motion-graphics aesthetic, no floating holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands, silhouettes and out-of-focus figures are allowed), no readable on-screen or printed text, no company logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.

PRIMARY ACCENT for this film: violet (hex #7C3AED) — the dominant practical lighting in every shot is violet (bench strips, status rings, screen spill); cyan (hex #00D4FF) appears only as a faint ambient fill deep in the background.

SHOT 1/6 — 15 s — Two of them: Split staging in one frame: the real robot on the floor at left; at right, a large monitor showing the soft defocused silhouette of its digital twin. Violet practicals bind the pair. A slow slider centers them.

SHOT 2/6 — 15 s — Sync: Static. The real arm moves through a slow reach — and the on-screen twin matches it frame for frame, silhouette to silhouette. A gentle rack focus trades the metal for the glass and back.

SHOT 3/6 — 15 s — Scrub: A hand rolls a physical jog wheel on the desk. The real robot smoothly reverses back through its last motion — time running backwards in metal — while the screen mirrors every reversed frame. Push-in on the pair.

SHOT 4/6 — 15 s — What-if: The screen twin diverges: it runs an alternate reach the real robot has never made, while the real machine holds perfectly still beside it — the future being rehearsed in glass. Macro on the screen's soft glow.

SHOT 5/6 — 15 s — Predict: A thermal-camera insert, photoreal FLIR look: one joint of the real robot glows warmer than its twins. On the monitor, the twin's matching joint pulses a soft early-warning halo. Slow rack focus between heat and glass.

SHOT 6/6 — 15 s — Hero: Robot and mirrored screen hold one matched pose, violet steady across both. A slow fifteen-degree orbit around the pair. Upper third clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO Twin (one continuous 15 s shot): A real robot and its on-screen twin move in perfect frame-for-frame sync; a jog wheel scrubs the metal machine backwards through time as the screen mirrors, the twin rehearses a reach the real robot never made, and a thermal insert warns of a warming joint — a slow orbit ending on the matched pair with clean space up top.
```

## Going longer (unlimited plan)

- **15 s is the per-shot ceiling** on Higgsfield's top video models — length comes
  from *sequencing shots*, not from one long generation. The 6-shot film above
  is the baseline ≈90 s cut.
- Every shot ends on a deliberate hold, so Higgsfield's **AI Video Extender** can
  stretch any shot past 15 s (or loop the hero shot indefinitely) without a visible seam.
- To add beats, duplicate a mid-film shot, change only the action sentence, and
  chain it via last-frame → first-frame. The STYLE block keeps it in the family.
- On Veo 3.1, use **scene extension** from the hero shot for arbitrarily long
  ambient loops of the end frame.

## Text to add in post (don't let the model render it)

- **Wordmark:** `OhhO` (first & last O cyan `#00D4FF`, middle `hh` white)
- **Product name:** OhhO Twin · **Tagline:** Your real robot. Mirrored in simulation. Live.
- **CTA:** `Open the twin →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
