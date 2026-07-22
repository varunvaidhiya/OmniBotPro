# OhhO Build — Higgsfield Video Prompt

- **Product:** OhhO Build — *Design any robot. For any industry.*
- **Category:** Design · **Primary accent:** Cyan `#00D4FF`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** Browser-based 3-D robot designer — drag in real parts, set requirements, get an AI-validated design + a sourced bill of materials.

## Full film — paste into Higgsfield storyboard mode (6 shots × 15 s ≈ 90 s)

Each `SHOT` below is one 15-second generation. Keep the STYLE and ACCENT paragraphs
at the top of **every** shot's prompt. In storyboard / long-video mode, let
Higgsfield carry continuity between shots; when generating shots one at a time,
feed each shot's **last frame** as the next shot's **first frame**.

```
STYLE — OhhO (identical for every film, do not change):
Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab / light-industrial space: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, tool walls, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm and 50mm prime lenses, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move per shot, no shake. Materials must read as physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections and fingerprint-level detail. Every monitor or tablet in frame shows only a soft, out-of-focus dark dashboard — never readable text. Mood: calm, confident, precise, quietly futuristic. Color grade: cool and high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated.
NEGATIVE: no CGI look, no 3-D render or motion-graphics aesthetic, no floating holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands, silhouettes and out-of-focus figures are allowed), no readable on-screen or printed text, no company logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.

PRIMARY ACCENT for this film: cyan (hex #00D4FF) — the dominant practical lighting in every shot is cyan (bench strips, status rings, screen spill); violet (hex #7C3AED) appears only as a faint ambient fill deep in the background.

SHOT 1/6 — 15 s — The parts: A slow motorized slider glides along a dark workbench where real robot parts are laid out in precise order — four mecanum wheels, anodized chassis plates, the joint modules of a six-axis arm, small camera pods. A cyan LED strip along the bench edge rims every part. Haze hangs in the key light. The move ends framing the empty center of the bench.

SHOT 2/6 — 15 s — The design: Over-the-shoulder of an engineer at a large monitor showing a soft out-of-focus dark 3-D design application, a robot model slowly rotating on screen. A hand measures a physical mecanum wheel with calipers, glancing between the part and the screen. Slow push-in toward the glass of the monitor, its cyan glow reflecting on the calipers.

SHOT 3/6 — 15 s — Assembly, base: Macro shot. Gloved hands bolt the mecanum wheels onto the chassis with an electric torque driver — each fastener seats with a clean click. Each wheel gets a slow spin-check by hand. The camera tracks low along the chassis edge, shallow focus riding the wheel hubs.

SHOT 4/6 — 15 s — Assembly, arm: The six-axis arm is lowered onto the mobile base and seated; connectors click home, a camera pod snaps onto the wrist. A small cyan status LED on the base blinks alive for the first time. A gimbal arcs slowly around the union of arm and base.

SHOT 5/6 — 15 s — Validation: One part on the bench sits under an amber-tinted work lamp — flagged. Hands swap it for an alternative part; the lamp light shifts to clean cyan. Behind, the defocused monitor's checklist column silently fills. Slow push-in as the swapped part is torqued down.

SHOT 6/6 — 15 s — Hero: The finished mobile manipulator stands centered on the cleared workbench, rimmed in cyan light, haze drifting above it. A slow fifteen-degree orbit; the robot's status LED breathes once. The upper third of the frame is clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO Build (one continuous 15 s shot): Gloved hands assemble a real mobile manipulator on a dark workbench — mecanum wheels torqued on, a six-axis arm seated, a camera pod clicked in — while a defocused design monitor glows beside the work; the finished robot's status LED breathes cyan as a slow push-in ends on a hero framing with clean dark negative space in the upper third.
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
- **Product name:** OhhO Build · **Tagline:** Design any robot. For any industry.
- **CTA:** `Launch the studio →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
