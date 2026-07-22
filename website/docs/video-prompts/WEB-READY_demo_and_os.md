# Web-ready generation sheet — OhhO hero demo + OhhO OS

Paste-ready prompts for generating the **two priority films** directly on the
**[higgsfield.ai](https://higgsfield.ai) website** (the free trial allows website
generation; it blocks the API/MCP, which is why these are formatted for manual
paste). Each `SHOT` block below is **one 15-second generation** — copy the whole
fenced block, paste it, generate, then move to the next shot. Assemble the shots
in order in any editor.

> These two films use the exact locked look from `README.md` (photoreal
> live-action, brand color only in practical lighting, all screens defocused so
> the films never date against the site UI). The full 21-product set lives in the
> other files in this folder; this sheet is just the two you asked to finish first.

---

## Higgsfield web UI settings (use for every shot)

| Setting | Value | Why |
|---|---|---|
| **Model** | **Seedance 2.0** (mode: **Standard**, not Fast) | Unlimited on your trial; best multi-shot continuity + native audio; Standard unlocks 1080p. |
| **Duration** | **15 s** (the maximum) | Longest single shot Seedance allows — you asked for the longest possible. Length beyond this comes from sequencing shots. |
| **Aspect ratio** | **16:9** | Landscape for the product-page player. |
| **Resolution** | **1080p** (or 4K if your trial exposes it in Standard mode) | Crisp, quality output. |
| **Audio** | **On** | Seedance generates native diegetic sound — servo whirs, fan hum, clicks. Keep it, or mute and lay a shared bed in the edit. |
| **Genre hint** | *auto* (or *epic* for the hero shots) | Optional cinematic bias. |
| **First frame / reference** | The keyframe still (below), on SHOT 1 and the Hero shot | Locks the look. Optional but recommended. |

**If Higgsfield suggests a preset** (e.g. "IN THE DARK") when you paste, choose
**"generate literally / use my prompt"** — the locked STYLE block is what keeps all
the films consistent.

**For the smoothest continuity:** if the web UI offers a **storyboard / multi-shot
(long-video) mode**, add these shots in sequence there and let it carry
continuity. Otherwise generate shot-by-shot and, where the model lets you, feed
the **previous shot's last frame as the next shot's start frame**.

---

## Step 0 — make the keyframe still first (recommended)

Model **Nano Banana Pro** (also unlimited on your trial), 16:9, 2K. Reuse the
result as the first-frame/reference image on SHOT 1 and the Hero shots, and as the
video `poster`.

```
Photorealistic cinematic still, a premium robotics brand film frame shot like an Apple product documentary. A modern robotics lab in near-darkness: polished concrete floor, dark matte walls, one aluminum-extrusion workbench rimmed by a cyan LED strip, a real wheeled mobile robot silhouetted in thin atmospheric haze, deep violet ambient fill far in the background. Full-frame 35mm cinema lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain, cool high-contrast grade with near-black shadows. The upper third of the frame is clean dark negative space. No text, no people, no faces, no logos, no CGI look — it must read as filmed footage. 16:9.
```

---

## Film 1 — OhhO hero / main product film (demo)

*The story: write one behavior, run it on any robot — wheeled base, arm, quadruped, drone — and the data compounds through a record → train → serve loop. This is the flagship film for the homepage hero, `/start` and `/why`.* Primary accent: **cyan**.

**8 shots × 15 s = 120 s.** Generate each block below as one clip, in order.

> Tip: shots 3, 4 and 5 (wheels / arm / legs) should feel like the *same* move on different bodies — reuse the same seed and keep the tracking speed identical so the rhyme reads.

**SHOT 1/8 — The desk**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — The desk: A dark robotics lab at night, haze in the air. One desk lamp pools over a mechanical keyboard; a pair of hands type a short block of code, the terminal screen a soft defocused cyan glow. Cyan LED practicals trace the room's edges. A slow cinematic push-in toward the enter key as a finger presses it.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 2/8 — Ship it**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Ship it: The enter key is pressed. A pulse of cyan light leaves the desk and runs along the floor cable runs, splitting cleanly three ways into the dark. The camera tracks the travelling light low across the polished concrete.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 3/8 — Body one — wheels**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Body one — wheels: A real wheeled mecanum-drive mobile base wakes, its status ring snapping cyan, and glides a smooth S-curve across the floor. A low tracking shot rides alongside the motion, wheels and rubber catching the light.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 4/8 — Body two — the arm**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Body two — the arm: A six-axis robot arm on a bench wakes with the identical cyan status ring and traces the same S-curve shape in the air above its base — the same behavior, expressed by a completely different body. A matched slow tracking move mirrors the previous shot.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 5/8 — Body three — legs**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Body three — legs: A four-legged quadruped robot wakes, ring cyan, and walks the same S-path across the floor — a third body performing the one shared behavior. Tracking alongside at low height, matching the rhythm of the previous two shots.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 6/8 — Record, train**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Record, train: One continuous dolly move through a doorway links two rooms: first a teleop bench where hands drive a leader arm as its follower twin mirrors every move under synced record lights — then a GPU server rack beyond, fans breathing under load in the dark. The record-to-train loop, told in one move.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 7/8 — Serve, compound**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Serve, compound: Back on the main floor, the wheeled robot repeats the learned pick-and-place task noticeably faster and cleaner than before — and a small drone spools up beside it, its ring snapping the same cyan as a fourth body joins the family. Wide framing, the fleet visibly growing.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 8/8 — Hero**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: cyan (hex #00D4FF) is the dominant practical light in every shot (bench strips, status rings, screen spill); violet (hex #7C3AED) only as faint background fill.

SHOT — Hero: All four robots — wheeled base, six-axis arm, quadruped and drone — arranged around the desk where it began, status rings breathing cyan in perfect unison, the terminal still glowing softly. A slow rising orbit pulls up and back through the haze and holds. The upper third of the frame is clean dark negative space for the wordmark.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```


---

## Film 2 — OhhO OS

*The story: one small identical engine drops into four completely different robots and they all run the same behavior in unison — the robot-agnostic runtime made literal.* Primary accent: **violet**.

**6 shots × 15 s = 90 s.** Generate each block below as one clip, in order.

> Tip: shots 2 and 3 (slotting the module in) carry the whole idea — keep the macro framing tight and the click moments clear.

**SHOT 1/6 — Four bodies**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — Four bodies: Four dark, powered-down robots stand in a row — a wheeled mobile base, a desk-mounted six-axis arm, a four-legged quadruped, and a drone on its landing pad — and in the foreground on the bench sits one small identical compute module. A slow motorized slider travels the length of the line-up in deep violet light, haze in the beams.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 2/6 — One engine**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — One engine: Macro shot. A pair of hands slot the small compute module into the wheeled base's chassis bay; it clicks home and the robot wakes, a violet status ring lighting up. A slow push-in on the first machine coming alive.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 3/6 — Same engine**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — Same engine: In calm succession, identical compute modules click into the arm, then the quadruped, then the drone — and each robot wakes with exactly the same violet status ring. The camera tracks slowly down the line as the lights come on one by one.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 4/6 — One behavior**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — One behavior: Wide shot. All four robots move at once — the same single motion expressed in four dialects: the base glides forward a metre, the arm traces the identical arc in the air, the quadruped walks the same path, the drone flies the same line. Perfect, deliberate unison, violet rings breathing together.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 5/6 — The stack**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — The stack: A defocused monitor beside the robots shows one soft glowing node-graph. The camera racks focus from the glass screen to the four synced machines beyond it — one runtime, four bodies, no seams — binding them together in a single slow arcing move.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```

**SHOT 6/6 — Hero**

```
STYLE: Photorealistic live-action cinematography — a premium robotics brand film shot like an Apple product documentary. Real physical robots and real hardware in a modern robotics lab: polished concrete floor, dark matte walls, aluminum-extrusion workbenches, neat cable runs, server racks and large monitors. All light comes from practical sources inside the space: one cool white key light, cyan LED strip accents (hex #00D4FF) and deep violet ambient fill (hex #7C3AED), with thin atmospheric haze catching the beams. Cinema-camera look: full-frame sensor, 35mm prime lens, shallow depth of field, filmic highlight roll-off, subtle natural film grain. The camera moves only on a motorized slider, gimbal or jib — one slow deliberate move, no shake. Materials read physically real: brushed aluminum, anodized black metal, rubber wheels, braided cables, glass screens with true reflections. Every monitor shows only a soft out-of-focus dark dashboard, never readable text. Mood: calm, confident, precise, quietly futuristic. Grade: cool, high-contrast, near-black shadows with neon cyan and violet practicals, never oversaturated. Audio: diegetic room sound only — servo whirs, fan hum, connector clicks, a low warm hum; no music with lyrics.

PRIMARY ACCENT: violet (hex #7C3AED) is the dominant practical light in every shot (bench strips, status rings, screen spill); cyan (hex #00D4FF) only as faint background fill.

SHOT — Hero: The four robots hold their formation, status rings breathing violet in unison, the small compute module's identical siblings glinting on the bench in the foreground. A slow rising orbit pulls up through the haze and holds. The upper third of the frame is clean dark negative space for the title.

NEGATIVE: no CGI look, no 3-D render or motion-graphics, no holograms, no wireframes, no cartoon or anime, no recognizable human faces (hands and silhouettes allowed), no readable on-screen or printed text, no logos or watermarks, no warm sunny tones, no oversaturation, no cuts inside a shot, no shaky handheld camera, no heavy lens flares.
```


---

## After generating

1. **Assemble** each film's shots in order in your editor (cut on the camera
   settling, or crossfade 6–10 frames for a seamless flow).
2. **Overlays** (never rendered by the model — composite in post): the `OhhO`
   wordmark (first & last **O** cyan `#00D4FF`, middle `hh` white), the product
   name + tagline over the Hero shot's negative space, the CTA, and
   `The Open Robotics Platform`. See the overlay table in `README.md`.
   - Demo CTA: `Start in 5 minutes →`
   - OS product name: **OhhO OS** · tagline: *The open-source engine for any robot.* · CTA: `Install the engine →`
3. **Export** muted H.264/WebM 16:9 to:
   - `website/public/videos/demo.mp4` (+ `demo.jpg` = the keyframe, as poster)
   - `website/public/videos/products/ohho-os.mp4` (+ `ohho-os.jpg` poster)
4. A short **loop cut of the Hero shot alone** works as the autoplay card /
   homepage background.

## Going longer

15 s is Seedance's per-shot ceiling, so the ~2-minute demo and ~90 s OS films get
their length from *sequencing the shots above*. To stretch further, run any shot
(especially a Hero hold) through Higgsfield's **AI Video Extender** to lengthen or
loop it seamlessly — every shot ends on a deliberate hold for exactly this.
