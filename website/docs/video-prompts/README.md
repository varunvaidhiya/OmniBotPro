# OhhO Product Video Prompts

AI-video generation prompts for a **product demo / introduction film** for every
one of the **nineteen OhhO products**. Each prompt is engineered so that the same
brand look comes out **regardless of which AI video model you paste it into** —
Sora, Veo, Runway, Kling, Luma, Pika, Hailuo, etc. — so the nineteen films feel
like one campaign when embedded on the product pages.

> Source of truth for what each product *does*: [`../products/`](../products/).
> Source of truth for the brand *look*: `website/app/globals.css`,
> `website/tailwind.config.ts`, `website/components/Hero.tsx`.

---

## The files

| File | What it is |
|---|---|
| `README.md` (this file) | The brand style bible, the shared shot template, per-platform setup, the post-production text overlays, and how to embed. **Read this first.** |
| `<slug>.md` × 19 | One ready-to-paste prompt per product (`build.md`, `frame.md`, … `proof.md`). Each is self-contained — copy the whole **Prompt** block into any tool. |
| `demo.md` | The **master hero / demo film** — the one-behavior-any-robot proof + the record→train→serve loop. Not a product spot; it tells the whole platform story and embeds on the homepage hero, `/start` and `/why`. Uses the same locked BRAND STYLE block, so it stays in the family. |

The 19 product prompt files:
`build` · `frame` · `bench` · `connect` · `bridge` · `serve` · `view` · `data` ·
`train` · `autonomy` · `mind` · `market` · `pilot` · `fleet` · `twin` · `care` ·
`comply` · `shield` · `proof`

---

## How cross-platform consistency is achieved

Different video models will never be *pixel*-identical, but you get a consistent
brand family by holding everything constant except the product. These prompts do
that with six locked levers — **use all six**:

1. **One identical BRAND STYLE block in every prompt.** The first paragraph of all
   19 prompts is byte-for-byte the same (palette hexes, environment, lighting,
   camera, mood, format, negatives). Only the second paragraph — the product
   SCENE — changes. Identical style text → consistent look.
2. **One canonical spec.** 16:9, 24 fps, ~8 seconds, one continuous camera move,
   no hard cuts. 8 s is the common denominator every major model can hit, so the
   films match in length and rhythm.
3. **Generate ONE brand keyframe still, then image-to-video everywhere.** This is
   the single biggest consistency lever. Make one "brand hero still" (see below),
   then feed it as the **first frame / reference image** in every tool that
   supports image-to-video (Runway, Kling, Luma, Pika, Hailuo, Veo). Text-only
   generation drifts; a shared first frame locks composition, palette and grade.
4. **Fix a seed and reuse it.** Pick one seed (e.g. `420024`) and keep it across
   products on tools that expose seeds. Vary only the SCENE text.
5. **Keep rendered text OUT of the model; add it in post.** Video models garble
   words and render fonts inconsistently. Generate clean footage with **open
   negative space** (the prompts leave room above the panel), then composite the
   real wordmark, product name, tagline and CTA in your editor using the actual
   brand fonts and hexes (table below). This guarantees the typography is
   identical across all 19.
6. **Bookend with one shared intro/outro sting + one LUT.** Add the same 0.6 s
   OhhO wordmark sting at the head and the same CTA card at the tail of every
   film in your editor, and apply one shared color LUT. Now all 19 open and close
   identically no matter which model rendered the middle.

---

## The BRAND STYLE block (identical in every prompt — do not edit)

This exact paragraph leads all 19 product prompts. It is reproduced here so you
can verify it never changes between products:

```
BRAND STYLE — OhhO (identical for every product, do not change):
Cinematic 3-D motion-graphics product spot, premium dark-tech keynote aesthetic
in the style of Apple, Vercel and Linear launch films. Environment: an infinite
deep navy-black void (hex #0A0E1A) with a faint glowing cyan perspective grid
receding into soft fog, fine floating dust particles, and gentle volumetric haze.
Two large soft light orbs breathe slowly: a cyan orb (hex #00D4FF) in the upper
left and a violet orb (hex #7C3AED) in the lower right. The centerpiece is a
single floating frosted-glass console panel — rounded corners, a thin bright
white top rim, a subtle accent-colored inner glow, true glassmorphism with
backdrop blur — showing a clean, minimal, high-tech UI. Lighting is dark and
moody with an accent-colored rim light, gentle bloom on every emissive element,
and a shallow depth of field. Camera: one single continuous, slow, smooth,
weighted move — a gentle push-in or a 15-degree orbit — with subtle parallax, no
cuts and no shake. Mood: calm, confident, premium, futuristic, uncluttered.
Color grade: cool, high-contrast neon accents on near-black, never oversaturated.
Render quality: ultra-detailed photoreal 3-D (Octane / Redshift grade), 4K,
24 fps, 16:9. Leave clean empty negative space above the panel for a title.
NEGATIVE: no people or faces (translucent stylized hands only where the prompt
explicitly asks), no real-world office or stock footage, no company logos or
watermarks, no garbled or gibberish text, no clutter, no fast cuts, no shaky
handheld camera, no excessive lens flare, no cartoon or anime style, no warm or
sunny tones, no oversaturation.
```

Each product prompt then appends a **SCENE** paragraph and a **PRIMARY ACCENT**
line. Cyan-accent products make cyan dominant (UI, rim light, icon glow) and keep
violet only as the small secondary orb; violet-accent products do the reverse.

---

## The shared shot template

Every prompt resolves to the same simple, reliable shape so all 19 cut together:

```
[0.0–1.5s]  Establish: the dark gridded void, breathing cyan + violet orbs, haze.
            Camera begins its slow continuous move toward the glass console.
[1.5–6.0s]  The product's ONE signature action plays inside / around the glass
            console (the SCENE) — elements assemble and resolve in the accent color.
[6.0–8.0s]  Everything settles into a clean hero composition; the accent glow
            pulses once; open negative space holds at top for the wordmark overlay.
```

For an **extended 16 s** cut (Sora and longer Kling/Luma extends), play the
signature action twice as slowly and add one extra detail beat — the structure
and grade stay identical.

---

## Make the brand keyframe still first (recommended)

Before generating any video, make **one** still you will reuse as the first
frame across tools. Use an image model (or the first frame of a Veo/Sora gen)
with this prompt:

```
The BRAND STYLE block above, composed as a single hero still: the frosted-glass
console panel floating slightly left of center in the dark cyan-gridded void,
cyan orb upper-left, violet orb lower-right, volumetric haze, clean empty space
in the upper third for a title. No text. 16:9, 4K, photoreal 3-D, cinematic.
```

Save it as `ohho-brand-keyframe.png`. Feed it as the **init / first-frame /
reference image** in Runway, Kling, Luma, Pika, Hailuo and Veo. Tools that only
do text-to-video (or where you want variety) still get the matching look from the
identical BRAND STYLE text.

---

## Per-platform quick setup

| Tool | Clip length | Aspect | First-frame image? | Notes |
|---|---|---|---|---|
| **OpenAI Sora** | up to ~20 s (longer on some tiers) | 16:9 / 9:16 / 1:1 | optional | Best prompt adherence; use the full prompt incl. the extended 16 s note. Use "remix" to keep a look and swap the SCENE per product. |
| **Google Veo 3 / 3.1** | ~8 s | 16:9 / 9:16 | yes (frames) | Hits the canonical 8 s exactly; strong physics. Has native audio — mute it or use the sound-design note. Keep on-screen text minimal. |
| **Runway Gen-4 / Turbo** | 5–10 s | 16:9 | **strongly yes** | Image-to-video from the brand keyframe gives the most consistent result. Set a fixed seed; set duration 8–10 s. |
| **Kling 2.x** | 5 s or 10 s | 16:9 | yes (start/end frame) | Excellent smooth orbit. Use the brand keyframe as start frame; 10 s mode for the extended cut. |
| **Luma Dream Machine / Ray** | 5–10 s | 16:9 | yes (keyframes) | Use the keyframe; "extend" to reach 8–10 s. |
| **Pika 2.x** | ~5–8 s | 16:9 | yes (ingredients) | Add the brand keyframe as an ingredient; keep the SCENE to one action for a 5 s clip. |
| **MiniMax Hailuo 02** | 6–10 s | 16:9 | yes | Strong motion; use the keyframe + fixed seed. |

**Universal settings:** 16:9, 24 fps, slow single camera move, fixed seed,
brand keyframe as first frame wherever supported.

---

## Post-production: the text overlays (add these in your editor)

Generate footage **without** relying on the model for text, then composite these
exact strings on the negative space using the brand fonts and hexes. This makes
the typography identical across all 19 films.

**Brand fonts:** `Space Grotesk` (wordmark + product name), `JetBrains Mono`
(eyebrow labels, CTA, mono UI), `Inter` (any body line).
**Hexes:** background `#0A0E1A`, cyan `#00D4FF`, violet `#7C3AED`, white `#FFFFFF`.
**Wordmark rule:** render `OhhO` with the **first and last `O` in cyan `#00D4FF`**
and the middle `hh` in white — exactly as on the site. Always end on
`The Open Robotics Platform`.

| Product | Product name (Space Grotesk) | Tagline (overlay) | CTA (JetBrains Mono) |
|---|---|---|---|
| Build | OhhO Build | Design any robot. For any industry. | Launch the studio → |
| Frame | OhhO Frame | Your robot stack, ready in one afternoon. | Open device console → |
| Bench | OhhO Bench | From a box of parts to a robot that powers on. | Open bring-up console → |
| Connect | OhhO Connect | One link. Any robot. Any transport. | Open the garage → |
| Bridge | OhhO Bridge | Connect any robot. Even the ones that don't speak ROS. | Open bridge console → |
| Serve | OhhO Serve | Robot AI inference, as an API. | Open the console → |
| View | OhhO View | Four cameras. One smart view. | Open perception viewer → |
| Data | OhhO Data | Collect. Label. Ship. | Open episode viewer → |
| Train | OhhO Train | Turn demonstrations into policies. | Open training console → |
| Autonomy | OhhO Autonomy | Map it, navigate it, command it in plain language. | Open mission control → |
| Mind | OhhO Mind | Give your robot a mind of its own. | Open the console → |
| Market | OhhO Market | Download a skill. Or sell one. | Open the marketplace → |
| Pilot | OhhO Pilot | Operate any robot. From anywhere. | Open the cockpit → |
| Fleet | OhhO Fleet | Update 50 robots like you update an app. | Open mission control → |
| Twin | OhhO Twin | Your real robot. Mirrored in simulation. Live. | Open the twin → |
| Care | OhhO Care | Fix it before it breaks. | Open maintenance → |
| Comply | OhhO Comply | Ship robots the regulators will pass. | Open compliance center → |
| Shield | OhhO Shield | Security for robots that touch the real world. | Open security dashboard → |
| Proof | OhhO Proof | Prove the robot is safe before it ships. | Open validation suite → |

**Suggested overlay timing (matches the 8 s template):**
`0.0–1.0s` OhhO wordmark sting (shared) → `1.0–6.0s` footage clean → `5.5–8.0s`
product name + tagline fade in on the negative space → last 1 s adds the CTA and
`The Open Robotics Platform`.

---

## Optional: sound design (if your tool adds audio, or for the edit)

Keep it consistent too: a low warm pad/drone, one soft "whoosh" as the camera
pushes in, a single clean UI "tick/chime" on the moment the SCENE resolves, and a
soft sub-bass swell on the accent-glow pulse. No music with lyrics. Same audio
bed across all 19.

---

## Accent + category reference (which color leads each film)

| Category | Product | Primary accent |
|---|---|---|
| Design | Build | **Cyan** `#00D4FF` |
| Foundation | Frame | **Violet** `#7C3AED` |
| Foundation | Bench | **Cyan** |
| Foundation | Connect | **Cyan** |
| Foundation | Bridge | **Violet** |
| Intelligence | Serve | **Cyan** |
| Intelligence | View | **Violet** |
| Intelligence | Data | **Cyan** |
| Intelligence | Train | **Cyan** |
| Intelligence | Autonomy | **Violet** |
| Intelligence | Mind | **Violet** |
| Intelligence | Market | **Cyan** |
| Operations | Pilot | **Violet** |
| Operations | Fleet | **Cyan** |
| Operations | Twin | **Violet** |
| Operations | Care | **Cyan** |
| Trust | Comply | **Violet** |
| Trust | Shield | **Cyan** |
| Trust | Proof | **Violet** |

---

## Embedding the videos on the product pages

Each product detail page renders a glass-card "console" slot — that is exactly
where the film goes. In
[`website/components/products/ProductDashboard.tsx`](../../components/products/ProductDashboard.tsx)
(rendered by `app/products/[slug]/page.tsx`), the per-slug mockup can be replaced
with the generated film:

```tsx
<video
  className="w-full h-full object-cover"
  src={`/videos/products/${slug}.mp4`}
  poster={`/videos/products/${slug}.jpg`}   // the brand keyframe still
  autoPlay muted loop playsInline
/>
```

Export each film **muted, looping, ≤ 8 s, H.264/WebM, 16:9**, drop it in
`website/public/videos/products/<slug>.mp4`, and use the brand keyframe as the
`poster`. Keep the existing caption (`product.dashboardCaption`) underneath.

---

## TL;DR workflow

1. Read this file. 2. Make one **brand keyframe still**. 3. Open a product file
(e.g. `serve.md`). 4. Paste its **Prompt** block into your tool, attach the
keyframe as the first frame, set 16:9 / 8 s / fixed seed. 5. Generate. 6. In your
editor, add the shared intro sting + the product's overlay text + CTA + the shared
LUT. 7. Export to `public/videos/products/<slug>.mp4`. Repeat for all 19 — the
locked style block + shared keyframe keep them a family.
</content>
</invoke>
