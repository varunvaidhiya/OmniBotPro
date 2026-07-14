# OhhO — Master Hero / Demo Film Prompt

- **Film:** OhhO hero demo — *the one-behavior-any-robot proof + the record→train→serve loop.* This is the **flagship brand film** (not one of the 19 product spots); it tells the whole platform story in one shot.
- **Category:** Brand / hero · **Primary accent:** Cyan `#00D4FF`
- **Canonical:** 16:9 · 24 fps · ~8 s · one continuous shot · fixed seed
- **The story it must land:** write a behavior once, run it on any robot (wheeled base, arm, quadruped) — and the data you collect compounds through a record → train → serve loop. Openness is the wedge; the compounding data is the moat.
- **Where it embeds:** the homepage hero background/slot (`components/Hero.tsx`), the `/start` "one behavior, any robot" section, and `/why`. Export to `website/public/videos/demo.mp4` with `demo.jpg` (the brand keyframe) as poster.

## Prompt — paste this whole block into any AI video tool

```
BRAND STYLE — OhhO (identical for every product, do not change):
Cinematic 3-D motion-graphics product spot, premium dark-tech keynote aesthetic in the style of Apple, Vercel and Linear launch films. Environment: an infinite deep navy-black void (hex #0A0E1A) with a faint glowing cyan perspective grid receding into soft fog, fine floating dust particles, and gentle volumetric haze. Two large soft light orbs breathe slowly: a cyan orb (hex #00D4FF) in the upper left and a violet orb (hex #7C3AED) in the lower right. The centerpiece is a single floating frosted-glass console panel — rounded corners, a thin bright white top rim, a subtle accent-colored inner glow, true glassmorphism with backdrop blur — showing a clean, minimal, high-tech UI. Lighting is dark and moody with an accent-colored rim light, gentle bloom on every emissive element, and a shallow depth of field. Camera: one single continuous, slow, smooth, weighted move — a gentle push-in or a 15-degree orbit — with subtle parallax, no cuts and no shake. Mood: calm, confident, premium, futuristic, uncluttered. Color grade: cool, high-contrast neon accents on near-black, never oversaturated. Render quality: ultra-detailed photoreal 3-D (Octane / Redshift grade), 4K, 24 fps, 16:9. Leave clean empty negative space above the panel for a title.
NEGATIVE: no people or faces (translucent stylized hands only where the prompt explicitly asks), no real-world office or stock footage, no company logos or watermarks, no garbled or gibberish text, no clutter, no fast cuts, no shaky handheld camera, no excessive lens flare, no cartoon or anime style, no warm or sunny tones, no oversaturation.

PRIMARY ACCENT for this film: cyan (#00D4FF) — the console UI, rim light, code stream and dominant glow all use cyan; violet (#7C3AED) appears only as the smaller secondary orb in the lower right.

SCENE — OhhO hero demo: A single luminous cyan line of code emerges from inside the glass console and flows outward, splitting cleanly into three ribbons of light. Each ribbon lands on a distinct translucent 3-D blueprint robot floating nearby — a wheeled mecanum base, a sleek 6-DOF robot arm, and a four-legged quadruped. As each ribbon connects, that robot lights up with synchronized cyan accents and performs one small, calm motion in unison — the base glides, the arm reaches, the quadruped takes a step — the same behavior expressed on three very different bodies. Meanwhile, on the console UI, a compact luminous loop diagram cycles once: glowing camera-frame glyphs stream in (RECORD), a rising policy curve resolves (TRAIN), and an endpoint node pulses outward (SERVE), the data flowing back into the central core so it visibly grows brighter — data compounding. All motion is smooth and weighted as the camera pushes slowly in.

END FRAME: the three synchronized blueprint robots settle calmly around the glass console, the record→train→serve loop resolved and the core glowing at its brightest, the cyan accent glow pulses once, clean empty negative space held in the upper third for a title. 16:9, 24 fps, ~8 seconds, one continuous camera move, fixed seed.
```

## Extended 16 s variant
Play the three ribbons connecting one at a time (base, then arm, then quadruped) at half speed so each embodiment reads clearly, then run the record→train→serve loop a full cycle. Add one detail beat: a fourth ribbon reaches out to a new translucent form (a drone) that lights up cyan and joins the synchronized motion — reinforcing "any robot." Same grade, same end frame.

## Text to add in post (don't let the model render it)
- **Wordmark:** `OhhO` (first & last O cyan `#00D4FF`, middle `hh` white)
- **Headline:** Open gets you in. Your data keeps you.
- **Sub / tagline:** One behavior, any robot — record → train → serve.
- **CTA:** `Start in 5 minutes →` · end on `The Open Robotics Platform`

**Suggested overlay timing (8 s template):** `0.0–1.0s` OhhO wordmark sting (shared) → `1.0–6.0s` footage clean → `5.5–8.0s` headline + tagline fade in on the negative space → last 1 s adds the CTA and `The Open Robotics Platform`.

## Platform cheat
Attach the brand keyframe as first frame; 16:9; 8 s; seed `420024`. Runway/Kling: image-to-video from the keyframe for the steadiest motion. This is the master film — use the extended 16 s note on Sora for the homepage hero cut, and export a clean 8 s loop for the `/start` proof section.
