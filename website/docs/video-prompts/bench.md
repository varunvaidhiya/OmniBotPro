# OhhO Bench — Product Video Prompt

- **Product:** OhhO Bench — *From a box of parts to a robot that powers on.*
- **Category:** Foundation · **Primary accent:** Cyan `#00D4FF`
- **Canonical:** 16:9 · 24 fps · ~8 s · one continuous shot · fixed seed
- **What it does:** Guided assembly, wiring, firmware flashing and hardware
  self-tests that prove every motor, sensor and servo works.

## Prompt — paste this whole block into any AI video tool

```
BRAND STYLE — OhhO (identical for every product, do not change):
Cinematic 3-D motion-graphics product spot, premium dark-tech keynote aesthetic in the style of Apple, Vercel and Linear launch films. Environment: an infinite deep navy-black void (hex #0A0E1A) with a faint glowing cyan perspective grid receding into soft fog, fine floating dust particles, and gentle volumetric haze. Two large soft light orbs breathe slowly: a cyan orb (hex #00D4FF) in the upper left and a violet orb (hex #7C3AED) in the lower right. The centerpiece is a single floating frosted-glass console panel — rounded corners, a thin bright white top rim, a subtle accent-colored inner glow, true glassmorphism with backdrop blur — showing a clean, minimal, high-tech UI. Lighting is dark and moody with an accent-colored rim light, gentle bloom on every emissive element, and a shallow depth of field. Camera: one single continuous, slow, smooth, weighted move — a gentle push-in or a 15-degree orbit — with subtle parallax, no cuts and no shake. Mood: calm, confident, premium, futuristic, uncluttered. Color grade: cool, high-contrast neon accents on near-black, never oversaturated. Render quality: ultra-detailed photoreal 3-D (Octane / Redshift grade), 4K, 24 fps, 16:9. Leave clean empty negative space above the panel for a title.
NEGATIVE: no people or faces (translucent stylized hands only where the prompt explicitly asks), no real-world office or stock footage, no company logos or watermarks, no garbled or gibberish text, no clutter, no fast cuts, no shaky handheld camera, no excessive lens flare, no cartoon or anime style, no warm or sunny tones, no oversaturation.

PRIMARY ACCENT for this film: cyan (#00D4FF) — the console UI, rim light, product icon and dominant glow all use cyan; violet (#7C3AED) appears only as the smaller secondary orb in the lower right.

SCENE — OhhO Bench: Inside the glass console, a clean wiring / harness diagram of a robot lights up trace by trace — motor board, arm bus, cameras and compute connecting in glowing cyan lines. Below it, a row of subsystem status tiles labeled by icon (motors, encoders, IMU, arm servos, cameras) flips one by one from dim grey to a bright cyan "OK", each flip emitting a soft pulse. A slim firmware-flash progress bar fills left to right and clicks to 100%. A glowing cyan line-art wrench icon hovers at the panel corner. The camera pushes slowly in as the final subsystem tile turns green and the whole board reads healthy.

END FRAME: every subsystem tile glowing cyan "OK" and the wiring diagram fully lit inside the glass console, the cyan accent glow pulses once, clean empty negative space held in the upper third for a title. 16:9, 24 fps, ~8 seconds, one continuous camera move, fixed seed.
```

## Extended 16 s variant
Add a calibration beat: a small wheel spins and an odometry dial centers, an IMU
bubble levels, an arm sweeps through its joints — each writing a green check.
Same grade, same end frame.

## Text to add in post (don't let the model render it)
- **Wordmark:** `OhhO` (first & last O cyan, middle `hh` white)
- **Product name:** OhhO Bench · **Tagline:** From a box of parts to a robot that powers on.
- **CTA:** `Open bring-up console →` · end on `The Open Robotics Platform`

## Platform cheat
Attach the brand keyframe as first frame; 16:9; 8 s; seed `420024`. Push-in suits
the "tiles turning green" reveal better than the orbit.
</content>
