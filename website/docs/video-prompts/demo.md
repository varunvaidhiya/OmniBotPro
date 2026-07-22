# OhhO — Master Hero / Demo Film — Higgsfield Video Prompt

- **Film:** OhhO hero demo — *the one-behavior-any-robot proof + the record→train→serve loop.* This is the **flagship brand film** (not one of the product spots); it tells the whole platform story.
- **Category:** Brand / hero · **Primary accent:** Cyan `#00D4FF`
- **Platform:** [Higgsfield.ai](https://higgsfield.ai) · storyboard / long-video mode · **8 shots × 15 s ≈ 120 s (2 min)** · 16:9 · 1080p+
- **Model pick:** Seedance 2.0 (default — 15 s shots, multi-shot continuity, native audio, unlimited-friendly) · Kling 3.0 (per-shot camera control) · Veo 3.1 (scene-extension chains)
- **The story it must land:** write a behavior once, run it on any robot (wheeled base, arm, quadruped, drone) — and the data you collect compounds through a record → train → serve loop. Openness is the wedge; the compounding data is the moat.
- **Where it embeds:** the homepage hero background/slot (`components/Hero.tsx`), the `/start` "one behavior, any robot" section, and `/why`. Export to `website/public/videos/demo.mp4` with `demo.jpg` (the photoreal keyframe) as poster.

## Full film — paste into Higgsfield storyboard mode (8 shots × 15 s ≈ 120 s)

Each `SHOT` below is one 15-second generation. Keep the STYLE and ACCENT paragraphs
at the top of **every** shot's prompt. In storyboard / long-video mode, let
Higgsfield carry continuity between shots; when generating shots one at a time,
feed each shot's **last frame** as the next shot's **first frame**. Shots 3–5 are
deliberately matched moves — generate them with the same seed so the "same
behavior, different body" rhyme reads clearly.

```
STYLE — OhhO (identical for every film, do not change):
Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab / light-industrial space: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, tool walls, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm and 50mm prime lenses, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move per shot, no shake. Materials must read as physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections and fingerprint-level detail. Every monitor or tablet in frame shows only a soft, out-of-focus dark dashboard — never readable text. Mood: calm, confident, precise, quietly futuristic. Color grade: cool and high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated.
NEGATIVE: no CGI look, no 3-D render or motion-graphics aesthetic, no floating holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands, silhouettes and out-of-focus figures are allowed), no readable on-screen or printed text, no company logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.

PRIMARY ACCENT for this film: cyan (hex #00D4FF) — the dominant practical lighting in every shot is cyan (bench strips, status rings, screen spill); violet (hex #7C3AED) appears only as a faint ambient fill deep in the background.

SHOT 1/8 — 15 s — The desk: A dark lab at night, haze in the air. One desk lamp pools over a mechanical keyboard; hands type a short block of code, the terminal a soft defocused glow. Cyan practicals trace the room's edges. A slow push-in toward the enter key.

SHOT 2/8 — 15 s — Ship it: The enter key is pressed. A pulse of cyan light leaves the desk and runs the floor cable runs, splitting cleanly three ways into the dark. The camera tracks the light low across the concrete.

SHOT 3/8 — 15 s — Body one — wheels: The wheeled mecanum base wakes, ring snapping cyan, and glides a smooth S-curve across the floor. A low tracking shot rides alongside the motion.

SHOT 4/8 — 15 s — Body two — the arm: The six-axis arm wakes with the same cyan ring and traces the identical S-curve in the air above its bench — the same behavior, spoken by a different body. A matched tracking move mirrors the previous shot.

SHOT 5/8 — 15 s — Body three — legs: The quadruped wakes, ring cyan, and walks the same S-path across the floor — third body, same single behavior. Tracking alongside, matching the rhythm of the previous two shots.

SHOT 6/8 — 15 s — Record, train: One continuous dolly through a doorway links two rooms: first the teleop bench, hands driving a leader arm as its follower mirrors under synced record lights — then the GPU rack beyond, breathing under load in the dark. The loop, in one move.

SHOT 7/8 — 15 s — Serve, compound: Back on the floor, the wheeled robot performs the learned task noticeably faster and cleaner than before — and a drone spools up beside it, ring snapping cyan as a fourth body joins the family. Wide, the fleet growing.

SHOT 8/8 — 15 s — Hero: All four robots — base, arm, quadruped, drone — arranged around the desk where it started, rings breathing cyan in unison, the terminal still glowing. A slow rising orbit pulls up through the haze. Upper third clean dark negative space for the wordmark. Hold.
```

## Single-clip fallback — one 15 s generation

```
SCENE — OhhO hero demo (one continuous 15 s shot): Hands type a short block of code at a night-lit desk and a cyan pulse splits three ways across the floor — a wheeled base glides an S-curve, a six-axis arm traces the same curve in the air, a quadruped walks it — then a teleop bench records, a GPU rack breathes, the robot returns faster, a drone joins, and a rising orbit ends on all four bodies breathing cyan around the desk with clean space up top.
```

## Going longer (unlimited plan)

- This is already the longest film in the set at ≈120 s. To push further, extend
  shots 3–5 (each body's behavior) with the **AI Video Extender**, or insert an
  extra body shot (e.g. a humanoid) between shots 5 and 6 using the same matched
  tracking move.
- The hero shot loops cleanly — use the extender's loop mode for an infinite
  homepage background.

## Text to add in post (don't let the model render it)

- **Wordmark:** `OhhO` (first & last O cyan `#00D4FF`, middle `hh` white)
- **CTA:** `Start in 5 minutes →` · end on `The Open Robotics Platform`
- **Suggested overlay timing (120 s film):** `0–2 s` OhhO wordmark sting (shared) →
  footage clean until ≈`100 s` → headline + tagline fade in over the hero shot's
  negative space → final 5 s adds the CTA and `The Open Robotics Platform`.

## Platform cheat

Higgsfield → Create → Video → storyboard / long-video mode. Model: Seedance 2.0
(or your plan's unlimited model). 16:9 · 1080p · 15 s per shot. Reuse the OhhO
photoreal keyframe still as the first frame of SHOT 1; keep one fixed seed across
shots 3–5 so the matched moves rhyme.
