# OhhO Autonomy — Product Video Prompt

- **Product:** OhhO Autonomy — *Map it, navigate it, command it in plain language.*
- **Category:** Intelligence · **Primary accent:** Violet `#7C3AED`
- **Canonical:** 16:9 · 24 fps · ~8 s · one continuous shot · fixed seed
- **What it does:** SLAM mapping + Nav2 navigation + a mission planner, driven by
  a natural-language agent that turns an instruction into a robot mission.

## Prompt — paste this whole block into any AI video tool

```
BRAND STYLE — OhhO (identical for every product, do not change):
Cinematic 3-D motion-graphics product spot, premium dark-tech keynote aesthetic in the style of Apple, Vercel and Linear launch films. Environment: an infinite deep navy-black void (hex #0A0E1A) with a faint glowing cyan perspective grid receding into soft fog, fine floating dust particles, and gentle volumetric haze. Two large soft light orbs breathe slowly: a cyan orb (hex #00D4FF) in the upper left and a violet orb (hex #7C3AED) in the lower right. The centerpiece is a single floating frosted-glass console panel — rounded corners, a thin bright white top rim, a subtle accent-colored inner glow, true glassmorphism with backdrop blur — showing a clean, minimal, high-tech UI. Lighting is dark and moody with an accent-colored rim light, gentle bloom on every emissive element, and a shallow depth of field. Camera: one single continuous, slow, smooth, weighted move — a gentle push-in or a 15-degree orbit — with subtle parallax, no cuts and no shake. Mood: calm, confident, premium, futuristic, uncluttered. Color grade: cool, high-contrast neon accents on near-black, never oversaturated. Render quality: ultra-detailed photoreal 3-D (Octane / Redshift grade), 4K, 24 fps, 16:9. Leave clean empty negative space above the panel for a title.
NEGATIVE: no people or faces (translucent stylized hands only where the prompt explicitly asks), no real-world office or stock footage, no company logos or watermarks, no garbled or gibberish text, no clutter, no fast cuts, no shaky handheld camera, no excessive lens flare, no cartoon or anime style, no warm or sunny tones, no oversaturation.

PRIMARY ACCENT for this film: violet (#7C3AED) — the console UI, rim light, product icon and dominant glow all use violet; cyan (#00D4FF) appears only as the smaller secondary orb in the upper left.

SCENE — OhhO Autonomy: Inside the glass console, a top-down SLAM floor-plan map draws itself line by line, walls and rooms resolving out of a point cloud. A short plain-language instruction chip appears and resolves into a small ordered mission sequence ("navigate → pick → return"). Then a glowing violet planned path snakes from a robot dot, curving smoothly around obstacles to a goal pin, with a soft costmap glow hugging the walls. A glowing violet line-art icon of connected route nodes hovers at the panel corner. The camera pushes slowly in as the path locks and the robot dot begins to glide along it.

END FRAME: the finished SLAM map with the glowing violet planned path from robot to goal pin and the resolved mission sequence inside the glass console, the violet accent glow pulses once, clean empty negative space held in the upper third for a title. 16:9, 24 fps, ~8 seconds, one continuous camera move, fixed seed.
```

## Extended 16 s variant
Add a beat where an obstacle appears mid-path and the violet route re-plans
around it live, then the control-mode mux toggles nav → AI → teleop. Same grade.

## Text to add in post (don't let the model render it)
- **Wordmark:** `OhhO` (first & last O cyan, middle `hh` white)
- **Product name:** OhhO Autonomy · **Tagline:** Map it, navigate it, command it in plain language.
- **CTA:** `Open mission control →` · end on `The Open Robotics Platform`

## Platform cheat
Attach the brand keyframe as first frame; 16:9; 8 s; seed `420024`. The
self-drawing map + path-around-obstacle is the hero motion.
</content>
