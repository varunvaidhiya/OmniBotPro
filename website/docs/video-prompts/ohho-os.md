# OhhO OS — Higgsfield Video Prompt

- **Product:** OhhO OS — *The open-source engine for any robot.*
- **Category:** Foundation · **Primary accent:** Violet `#7C3AED`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** The robot-agnostic runtime that powers every OhhO product. One engine connects to anything on wheels, legs or wings, carrying the whole stack from perception to training.

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

SHOT 1/6 — 15 s — Four bodies: Four dark robots in a row — a wheeled base, a desk-mounted arm, a quadruped, a drone on its pad — and in the foreground, one small identical compute module on the bench. A slow slider travels the line-up in violet light.

SHOT 2/6 — 15 s — One engine: Macro. Hands slot the compute module into the wheeled base's bay; it clicks home and the robot wakes with a violet ring. Push-in on the first machine coming alive.

SHOT 3/6 — 15 s — Same engine: In calm succession, identical modules click into the arm, the quadruped and the drone — and each wakes with exactly the same violet ring. The camera tracks down the line as the lights come on one by one.

SHOT 4/6 — 15 s — One behavior: Wide. All four move at once, the same motion in four dialects: the base glides a metre, the arm traces the identical arc in the air, the quadruped walks the same path, the drone flies the same line. Perfect unison.

SHOT 5/6 — 15 s — The stack: A defocused monitor shows one soft node-graph glow; the camera racks from the glass to the four synced machines beyond it — one runtime, four bodies, no seams. A slow arc binds them in one move.

SHOT 6/6 — 15 s — Hero: The four robots hold formation, rings breathing violet in unison, the little module's siblings glinting on the bench. A slow rising orbit. Upper third clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO OS (one continuous 15 s shot): One small compute module clicks into a wheeled base, then identical modules wake an arm, a quadruped and a drone with the same violet ring — and all four perform one motion in four dialects, gliding, tracing, walking and flying the same line — a rising orbit ending on the formation breathing in unison with clean space up top.
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
- **Product name:** OhhO OS · **Tagline:** The open-source engine for any robot.
- **CTA:** `Install the engine →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
