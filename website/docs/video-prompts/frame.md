# OhhO Frame — Higgsfield Video Prompt

- **Product:** OhhO Frame — *Your robot stack, ready in one afternoon.*
- **Category:** Foundation · **Primary accent:** Violet `#7C3AED`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** A production-grade robot software foundation — Docker, ROS 2, simulation and CI/CD, pre-wired, single or multi-machine.

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

SHOT 1/6 — 15 s — Bare metal: A dark rack corner: a bare credit-card-size compute board on an antistatic mat, a GPU tower idling beside it, coiled cables, violet strip light tracing the rack rails. A slow slider move drifts across the hardware, haze in the beam.

SHOT 2/6 — 15 s — Seating the brain: Macro. Hands seat the compute board into the robot's chassis bay — standoffs screwed, ribbon connectors clicked home one by one. Slow push-in as the last connector seats and a tiny power LED wakes violet.

SHOT 3/6 — 15 s — Boot cascade: The power switch flips. Board LEDs cascade on in sequence; the robot's depth camera wakes, its faint projector glow visible in the haze; the wheels twitch a brief self-test. Static tripod with a very slow zoom toward the waking machine.

SHOT 4/6 — 15 s — The stack comes up: A monitor beside the robot scrolls a defocused terminal, log lines flowing as soft light. The robot rotates calmly in place as its software stack comes alive. A jib rises slowly from bench height to a high angle over robot and screen.

SHOT 5/6 — 15 s — Multi-machine: The camera dollies along a neatly-clipped ethernet run across the floor — from the robot's compute bay all the way to the glowing GPU tower across the room — link LEDs pulsing in sync at both ends. Violet practicals pool along the cable path.

SHOT 6/6 — 15 s — Hero: The robot idles ready beside the rack, its status LED breathing violet, the GPU tower glowing behind. A slow fifteen-degree orbit. The upper third of the frame is clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO Frame (one continuous 15 s shot): Hands seat a small compute board into a real robot's chassis; the power flips and LEDs cascade on, the depth camera's projector glow blooms in the haze, a defocused terminal scrolls on a monitor beside it, and a slow orbit ends on the ready robot breathing violet with clean negative space in the upper third.
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
- **Product name:** OhhO Frame · **Tagline:** Your robot stack, ready in one afternoon.
- **CTA:** `Open device console →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
