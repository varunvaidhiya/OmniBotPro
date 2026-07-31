"use client";

/*
 * LogoMarquee — the homepage "Built on the open stack" logo wall.
 *
 * A two-row infinite scroller of brand wordmarks representing the OSS
 * projects, runtimes, standards and robots the OhhO platform is built
 * with. Each chip carries a small brand-color accent dot so the wall
 * reads as a color spectrum without becoming visually noisy (the site
 * theme is dark cyan/violet — colored text chips would clash).
 *
 * Inserted between StatsBar and BrandAgnostic on the homepage:
 *   hero → proof stats → "built with" logo wall → open-ecosystem moat.
 *
 * Accessibility + perf:
 *   - Honors prefers-reduced-motion (marquee-track animation gated in
 *     globals.css via the global reduce block).
 *   - Honors [data-perf="lite"] (animation dropped for low-power tiers).
 *   - Pauses on hover (.marquee-pause:hover) so users can read a brand.
 *   - Edge fade via mask-image so chips glide in/out instead of hard-cut.
 *
 * Brand context:
 *   Per website/AGENTS.md §2, marketing copy must stay brand-agnostic. This
 *   section is the ONE sanctioned surface that names real brands — it
 *   reads as "the open stack OhhO is built with," not "our partners."
 *   Only brands that appear as real dependencies in pyproject.toml /
 *   requirements.txt / package.json / infra/ are listed here.
 *
 * See docs/market analysis/11-partners.md for the aspirational partner
 * list — those are not yet signed and are deliberately NOT shown here.
 */

import { useScrollReveal } from "@/hooks/useScrollReveal";

interface Brand {
  name: string;
  accent: string;
  category: string;
}

// Row 1 — AI, simulation, training, standards (scrolls left)
const ROW_TOP: Brand[] = [
  { name: "NVIDIA Isaac", accent: "#76b900", category: "Simulation" },
  { name: "Gazebo", accent: "#f69321", category: "Simulation (ROS 2)" },
  { name: "PyTorch", accent: "#ee4c2c", category: "Deep learning" },
  { name: "Hugging Face LeRobot", accent: "#ffd21e", category: "VLA datasets" },
  { name: "ONNX", accent: "#00a1f1", category: "Model exchange" },
  { name: "Anthropic Claude", accent: "#cc785c", category: "Mind reasoner" },
  { name: "LangGraph", accent: "#4ade80", category: "Agent loop" },
  { name: "FastAPI", accent: "#05998b", category: "Serve API" },
  { name: "Weights & Biases", accent: "#ffb400", category: "Experiment tracking" },
  { name: "Cyclone DDS", accent: "#6d4aff", category: "Robot protocol" },
  { name: "MAVLink", accent: "#2a8fed", category: "Drone protocol" },
  { name: "Open-RMF", accent: "#f59e0b", category: "Fleet coordination" },
];

// Row 2 — Web platform, cloud, robot hardware, VR, observability (scrolls right)
const ROW_BOTTOM: Brand[] = [
  { name: "Next.js + Vercel", accent: "#ffffff", category: "Web platform" },
  { name: "Supabase", accent: "#3ecf8e", category: "Auth + database" },
  { name: "Firebase", accent: "#ffca28", category: "Analytics" },
  { name: "Stripe", accent: "#635bff", category: "Billing" },
  { name: "Three.js", accent: "#e2e8f0", category: "3-D hero scene" },
  { name: "Unitree", accent: "#f87171", category: "G1 · H1 · Go2" },
  { name: "Trossen / SO-101", accent: "#60a5fa", category: "Reference arm" },
  { name: "Unity", accent: "#cbd5e1", category: "VR engine" },
  { name: "OpenXR", accent: "#00a4e4", category: "XR standard" },
  { name: "Meta Quest", accent: "#1b69de", category: "MR headset" },
  { name: "Grafana", accent: "#f46800", category: "Observability" },
  { name: "Docker", accent: "#2496ed", category: "Containerization" },
];

export default function LogoMarquee() {
  const { ref, inView } = useScrollReveal();

  return (
    <section id="stack" style={{ padding: "72px 0 88px" }}>
      <div className="max-w-content mx-auto px-6">
        <div
          ref={ref}
          className="text-center max-w-[620px] mx-auto mb-[48px] transition-all duration-[650ms]"
          style={{ opacity: inView ? 1 : 0, transform: inView ? "none" : "translateY(22px)" }}
        >
          <div
            className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]"
            style={{ color: "var(--cyan)" }}
          >
            Built on the open stack
          </div>
          <h2 className="font-display font-bold text-[clamp(26px,3.6vw,38px)] tracking-tight leading-[1.12] mb-4 legible">
            The same open tools you<br />
            <span className="text-cyan">already know and trust.</span>
          </h2>
          <p className="text-[15px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            OhhO is built on the open-source foundation the robotics world
            already runs on — no proprietary lock-in, no black boxes. We extend
            the ecosystem; we don&rsquo;t wall it.
          </p>
        </div>
      </div>

      <div
        className="marquee-pause"
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        <MarqueeRow brands={ROW_TOP} reverse={false} />
        <MarqueeRow brands={ROW_BOTTOM} reverse={true} />
      </div>
    </section>
  );
}

function MarqueeRow({ brands, reverse }: { brands: Brand[]; reverse: boolean }) {
  // Duplicate the brand list once per row so the track is 2× wide; the
  // marquee-scroll keyframe translates the track by -50%, which lands
  // on the exact start of the second copy — a seamless infinite loop.
  const doubled = [...brands, ...brands];
  return (
    <div className="marquee-viewport">
      <div className={`marquee-track${reverse ? " reverse" : ""}`}>
        {doubled.map((b, i) => (
          <BrandChip key={`${b.name}-${i}`} brand={b} />
        ))}
      </div>
    </div>
  );
}

function BrandChip({ brand }: { brand: Brand }) {
  return (
    <div
      className="flex flex-col gap-1 flex-shrink-0"
      style={{
        padding: "14px 22px",
        margin: "0 6px",
        borderRadius: "14px",
        background: "rgba(255,255,255,0.025)",
        border: "1px solid rgba(255,255,255,0.08)",
        minWidth: "168px",
        minHeight: "72px",
        justifyContent: "center",
        alignItems: "center",
        textAlign: "center",
      }}
    >
      <div className="flex items-center gap-2">
        <span
          className="inline-block rounded-full flex-shrink-0"
          style={{ width: "6px", height: "6px", background: brand.accent }}
          aria-hidden
        />
        <span
          className="font-display text-[14.5px] font-semibold legible whitespace-nowrap"
          style={{ color: "rgba(255,255,255,0.88)" }}
        >
          {brand.name}
        </span>
      </div>
      <div
        className="font-mono text-[10px] tracking-[0.04em] uppercase"
        style={{ color: "rgba(255,255,255,0.42)" }}
      >
        {brand.category}
      </div>
    </div>
  );
}