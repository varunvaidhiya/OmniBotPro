# OhhO Care — Higgsfield Video Prompt

- **Product:** OhhO Care — *Fix it before it breaks.*
- **Category:** Operations · **Primary accent:** Cyan `#00D4FF`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** Predictive maintenance + service workflow — degradation signals become work orders, parts auto-sourced from the BOM, repairs logged.

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

SHOT 1/6 — 15 s — The wobble: Extreme macro tracking a single wheel module as it rolls: the faintest wobble in the hub, a shiver of vibration in the shallow focus — a fault only a machine would notice. Cyan practicals streak the rim.

SHOT 2/6 — 15 s — Seen in heat: A photoreal thermal-camera view: four motors in cool blues — one blooming hot orange. Cut back to the normal view: the robot rolls on, outwardly perfect. The problem is invisible, and already known.

SHOT 3/6 — 15 s — The order: A wall dashboard blurs amber in the bokeh. A labeled parts drawer slides open under a hand that lifts out exactly one replacement servo, its bag catching the cyan light. Push-in on the chosen part.

SHOT 4/6 — 15 s — The fix: Overhead macro. Gloved hands swap the module — electric driver out, click, new module in, torque wrench confirming with a clean click-click. Fast, ordered, practiced.

SHOT 5/6 — 15 s — True again: The identical macro tracking shot from the opening: the wheel now rolls perfectly true, hub steady, no shimmer. The thermal insert flashes once — four motors, all cool blue.

SHOT 6/6 — 15 s — Hero: The robot rolls smoothly past the bench where the tools rest, status light steady cyan. A slow fifteen-degree orbit as it passes. Upper third clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO Care (one continuous 15 s shot): A macro shot catches the faintest wobble in a rolling wheel; a thermal view shows one motor blooming hot among cool blues; a hand lifts the exact replacement from a parts drawer and gloved hands swap it with torque-wrench clicks — the same macro now rolling perfectly true, a slow orbit ending on the healthy robot with clean space up top.
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
- **Product name:** OhhO Care · **Tagline:** Fix it before it breaks.
- **CTA:** `Open maintenance →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
