# OhhO Data — Higgsfield Video Prompt

- **Product:** OhhO Data — *Collect. Label. Ship.*
- **Category:** Intelligence · **Primary accent:** Cyan `#00D4FF`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **6 shots × 15 s ≈ 90 s** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **What it does:** End-to-end demonstration-data pipeline — teleop recording, multi-camera sync, episode viewer, LeRobot-format datasets, training-ready.

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

SHOT 1/6 — 15 s — The rig: A teleop bench in cyan practicals: a leader arm on the left, its follower twin on the right, small camera rigs overhead on arms, tally lights dark. A slow slider surveys the paired setup.

SHOT 2/6 — 15 s — Rolling: Hands grip the leader arm; every tally light snaps cyan at once. The follower mirrors a smooth pick in perfect time, the overhead rig cameras blinking record in sync. Wide, both arms in frame.

SHOT 3/6 — 15 s — Lockstep: Macro rack focus alternating between the operator's hand on the leader and the follower's gripper — identical motion, identical timing, two machines one gesture.

SHOT 4/6 — 15 s — Episodes: Static frame. The same pick repeats in smooth takes; after each, the tally double-blinks — episode saved. One sloppy take ends in a long amber blink — discarded. The rhythm of curation, told in lights.

SHOT 5/6 — 15 s — Shipping: A defocused monitor shows a timeline of clips filling left to right. A hand taps a final key; drive-activity LEDs flicker in a fast, tidy burst as the dataset packs. Slow push-in on the glowing drives.

SHOT 6/6 — 15 s — Hero: The paired arms at rest in matched poses, tally lights steady cyan, the monitor glowing soft behind. A slow fifteen-degree orbit. Upper third clean dark negative space for a title. Hold.
```

## Single-clip fallback — one 15 s generation

For a quick single-clip version (or models capped below 15 s), paste the STYLE and
ACCENT paragraphs above, then this condensed scene:

```
SCENE — OhhO Data (one continuous 15 s shot): On a teleop bench, hands drive a leader arm while its follower mirrors every move under synced overhead record lights — good takes double-blink saved, one sloppy take blinks amber discarded — then drive LEDs flicker as the dataset packs, ending in a slow orbit on the resting paired arms with clean negative space up top.
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
- **Product name:** OhhO Data · **Tagline:** Collect. Label. Ship.
- **CTA:** `Open episode viewer →` · end on `The Open Robotics Platform`

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
all films where the model exposes it.
