# OhhO Product Video Prompts — Higgsfield.ai

AI-video generation prompts for a **product demo / introduction film** for every
OhhO product, written for **[Higgsfield.ai](https://higgsfield.ai)** — the
platform these films are generated on. The prompts are built around what
Higgsfield does best: **multi-shot storyboard films** sequenced from **15-second
shots**, photoreal model output, and an **unlimited plan** that makes long,
many-shot films free to iterate on.

> Source of truth for what each product *does*: [`../products/`](../products/).
> The look is deliberately **photoreal live-action** — NOT the website's current
> UI or a CGI/motion-graphics style. The site's UI will change; the films must
> not date with it. Screens in frame are always soft and out of focus.

---

## The files

| File | What it is |
|---|---|
| `README.md` (this file) | The Higgsfield playbook: platform capabilities, the locked photoreal STYLE block, the shot grammar, post-production overlays, and how to embed. **Read this first.** |
| `<slug>.md` × 21 | One ready-to-paste storyboard per product (`build.md`, `frame.md`, … `proof.md`, plus `link.md` and `ohho-os.md`). Each is self-contained — **6 shots × 15 s ≈ 90 s** per film. |
| `demo.md` | The **master hero / demo film** — one-behavior-any-robot + the record→train→serve loop. **8 shots × 15 s ≈ 2 minutes.** Embeds on the homepage hero, `/start` and `/why`. |

The 21 product prompt files:
`build` · `frame` · `bench` · `connect` · `bridge` · `link` · `ohho-os` · `serve` ·
`view` · `data` · `train` · `autonomy` · `mind` · `market` · `pilot` · `fleet` ·
`twin` · `care` · `comply` · `shield` · `proof`

---

## How Higgsfield works (and how these prompts use it)

Higgsfield is a multi-model AI video platform. The relevant capabilities:

| Capability | What it means for these films |
|---|---|
| **15 s per shot** | The top video models on Higgsfield (Seedance 2.0, Kling 3.0, Wan) generate up to **~15 seconds per clip**. That is the atom of these prompts — every `SHOT` block is one 15 s generation. |
| **Storyboard / long-video mode** | Higgsfield sequences shots into **multi-shot films minutes long**, holding continuity between shots (scene extension, first/last-frame transitions). Each product file is written as a ready-to-paste storyboard. |
| **AI Video Extender** | Extends any clip past its generation cap, loops it, or continues it seamlessly. Every shot in these prompts ends on a deliberate **hold** so extension has a clean seam. |
| **Unlimited plan** | Unlimited generations on the plan's selected model(s) means long films are cheap to iterate: regenerate individual shots freely, keep the best take of each, and assemble. |
| **Image-to-video** | Feeding a shared photoreal keyframe still as the first frame locks composition, palette and grade across films (see below). |
| **Camera presets** | Higgsfield's camera-control presets (dolly, orbit, crane/jib, tracking) map 1:1 to the single move each SHOT names — pick the matching preset instead of relying on text alone where available. |

**Model picks** (in order of preference for this campaign):

| Model | Why / when |
|---|---|
| **Seedance 2.0** | Default. Best multi-shot continuity, native audio, 1080p, 15 s shots — and the model family Higgsfield runs unlimited promotions on. |
| **Kling 3.0** | Per-shot camera control and storyboard mode — use when a shot's camera move needs to be exact (the matched moves in `demo.md`). |
| **Veo 3.1** | Scene extension and first/last-frame transitions, up to 4K — use for chaining and for arbitrarily long ambient loops of a hero shot. |
| **Sora 2 / Wan / Minimax** | Alternates; same prompts work. Wan for lip-sync-free product motion, Minimax for fast iterations. |

Use whichever of these your plan's unlimited selection covers — the prompts are
model-agnostic on purpose.

---

## The look: photoreal live-action (not CGI, not the site's UI)

Earlier versions of these prompts rendered a stylized 3-D "glass console" UI.
That is retired. The films are now **photorealistic live-action**: real robots,
real hands, real benches, cinema-camera grammar. Three rules keep them on-brand
and future-proof:

1. **No readable UI anywhere.** Every monitor, tablet or terminal in frame is a
   soft out-of-focus glow. The website's UI will change; the films won't date.
2. **Brand color lives in practical light.** Cyan `#00D4FF` and violet `#7C3AED`
   appear only as physical light sources — LED strips, status rings, screen
   spill — never as graphics. Each film is led by one of the two accents.
3. **It must read as filmed footage.** The NEGATIVE list bans the CGI/render
   look, holograms, wireframes and motion graphics outright.

---

## The STYLE block (identical in every prompt — do not edit)

This exact text leads **every shot of every film**. It is reproduced here so you
can verify it never changes between products:

```
STYLE — OhhO (identical for every film, do not change):
Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab / light-industrial space: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, tool walls, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm and 50mm prime lenses, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move per shot, no shake. Materials must read as physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections and fingerprint-level detail. Every monitor or tablet in frame shows only a soft, out-of-focus dark dashboard — never readable text. Mood: calm, confident, precise, quietly futuristic. Color grade: cool and high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated.
NEGATIVE: no CGI look, no 3-D render or motion-graphics aesthetic, no floating holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands, silhouettes and out-of-focus figures are allowed), no readable on-screen or printed text, no company logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

Each film then adds a one-line **PRIMARY ACCENT** paragraph (cyan-led or
violet-led practicals) and its numbered **SHOT** blocks.

---

## The shot grammar

Every product film is **6 shots × 15 s ≈ 90 s** (the hero `demo.md` is 8 shots ≈
2 min), and every film follows the same arc so the campaign cuts together:

```
SHOT 1  Establish     — the space, the hardware, the accent practicals, haze.
SHOT 2  First action  — the product's story begins with one concrete physical act.
SHOT 3  The mechanism — the signature capability, shown not told.
SHOT 4  Depth beat    — a second angle on the capability (scale, precision, contrast).
SHOT 5  The differentiator — the beat only this product can claim (recovery,
        swap, verification, publish…).
SHOT 6  Hero          — a slow orbit/push on the resolved scene; accent light
        breathes once; the UPPER THIRD is held as clean dark negative space
        for the title overlay. Ends on a hold (extender-friendly).
```

One camera move per shot, named explicitly (slider, dolly, gimbal arc, jib,
tracking, orbit, push-in) — matching Higgsfield's camera presets.

---

## Make the photoreal keyframe still first (recommended)

Generate **one** still with Higgsfield's image model and reuse it as the
first frame of SHOT 1 across all films:

```
The full STYLE paragraph above, composed as a single hero still: a modern
robotics lab in near-darkness, polished concrete,
one workbench rimmed by a cyan LED strip, a mobile robot silhouetted in haze,
violet ambient fill deep in the background, cinema-lens shallow depth of field,
photoreal. The upper third of the frame is clean dark negative space. No text,
no faces. 16:9, photorealistic, filmic grain.
```

Save it as `ohho-photoreal-keyframe.png` and attach it as the image-to-video
first frame. It replaces the old CGI "brand keyframe".

---

## Higgsfield workflow (per film)

1. Open the product's `.md` file; copy the full storyboard block.
2. Higgsfield → Create → Video → **storyboard / long-video mode** → model
   **Seedance 2.0** (or your unlimited model) → 16:9 · 1080p · 15 s per shot.
3. Attach `ohho-photoreal-keyframe.png` as SHOT 1's first frame; fix a seed
   where the model exposes one and reuse it across films.
4. Generate. On the unlimited plan, **regenerate individual shots freely** and
   keep the best take of each — takes are free, taste is the budget.
5. Chain: storyboard mode handles transitions; generating shot-by-shot, feed
   each shot's last frame as the next shot's first frame.
6. Stretch: use the **AI Video Extender** on any shot that needs more air, and
   loop the hero shot for ambient/background use.
7. Assemble in your editor: shared 2 s wordmark sting at the head, the product's
   text overlays (below) on the hero shot, shared CTA card, one shared LUT.

---

## Post-production: the text overlays (add these in your editor)

Never let the model render text. Composite these exact strings over the hero
shot's negative space using the brand fonts and hexes.

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
| Link | OhhO Link | Connect any AI agent to your robots. | View the catalog → |
| OS | OhhO OS | The open-source engine for any robot. | Install the engine → |
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

**Suggested overlay timing (90 s film):** `0–2 s` OhhO wordmark sting (shared) →
footage clean until the hero shot (≈`75 s`) → product name + tagline fade in on
the negative space → final 5 s adds the CTA and `The Open Robotics Platform`.

---

## Sound design

Seedance 2.0 and Veo generate native audio — either keep the diegetic sound
(servo whirs, fan hum, connector clicks suit the photoreal look well) or mute
and lay one shared bed in the edit: a low warm pad, room tone, real mechanical
foley on the action beats, one soft sub swell on the hero light-breathe. No
music with lyrics. Same bed across all films.

---

## Accent + category reference (which color leads each film)

| Category | Product | Primary accent |
|---|---|---|
| Design | Build | **Cyan** `#00D4FF` |
| Foundation | Frame | **Violet** `#7C3AED` |
| Foundation | Bench | **Cyan** |
| Foundation | Connect | **Cyan** |
| Foundation | Bridge | **Violet** |
| Foundation | Link | **Cyan** |
| Foundation | OS | **Violet** |
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

Each product detail page renders a glass-card "console" slot — that is where the
film goes. In
[`website/components/products/ProductDashboard.tsx`](../../components/products/ProductDashboard.tsx)
(rendered by `app/products/[slug]/page.tsx`), the per-slug mockup can be replaced
with the generated film:

```tsx
<video
  className="w-full h-full object-cover"
  src={`/videos/products/${slug}.mp4`}
  poster={`/videos/products/${slug}.jpg`}   // the photoreal keyframe still
  autoPlay muted loop playsInline
/>
```

Two exports per product:
- **Card loop** — the hero shot alone (15 s, or extender-looped), muted H.264/WebM,
  for the autoplay slot above: `public/videos/products/<slug>.mp4`.
- **Full film** — the complete ≈90 s cut with overlays and sound, for the product
  page's expanded player / social: `public/videos/products/<slug>-full.mp4`.

The 2-minute `demo` film exports to `public/videos/demo.mp4` (full) plus a
looping hero-shot cut for the homepage background.

---

## TL;DR workflow

1. Read this file. 2. Make the **photoreal keyframe still**. 3. Open a product
file (e.g. `serve.md`). 4. Paste its storyboard into Higgsfield's storyboard /
long-video mode — Seedance 2.0, 16:9, 1080p, 15 s per shot, keyframe as first
frame. 5. Generate; on unlimited, re-roll shots until each take is right.
6. Extend / loop with the AI Video Extender where needed. 7. Add the shared
sting + overlays + LUT in the edit. 8. Export the card loop and the full film
to `public/videos/products/`. Repeat for all 21 + the demo — the locked STYLE
block and shared keyframe keep them one campaign.
