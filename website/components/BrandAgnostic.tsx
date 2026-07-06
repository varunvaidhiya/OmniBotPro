"use client";

/*
 * BrandAgnostic — the homepage "open-source robots first" moat section.
 *
 * This is the single most important marketing claim on the site. OhhO targets
 * open-source robotics: open-source robots and open-source robot hardware are
 * the primary focus. It also works with any commercial brand that keeps its
 * software layer open source and welcomes third-party developers into its
 * ecosystem. Either way, there's no vendor lock-in — the customer keeps pure
 * value: the best open-source robotics, for the cheapest price, under one
 * umbrella.
 *
 * Rendered between StatsBar and OhhoOS so the narrative reads:
 *   hero → proof → "open-source first, open ecosystems welcome" → engine → products.
 *
 * Per the website AGENTS.md rules, marketing copy stays brand-free: chips use
 * robot-class / standards / capability language, never commercial brand names.
 */

import { motion } from "framer-motion";
import {
  Cpu,
  Bot,
  BrainCircuit,
  Cloud,
  Cable,
  Unlock,
  ShieldCheck,
  ArrowRight,
} from "lucide-react";
import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";
import { GITHUB_HREF } from "@/lib/site";

// "Bring your own ___" — each card is one layer of the stack the customer
// is free to swap. Per the website AGENTS.md rules, marketing copy stays
// brand-free: chips use robot-class / standards / capability language, never
// commercial brand names. "Any" is made concrete with categories, not logos.
const LAYERS = [
  {
    title: "Any compute",
    icon: Cpu,
    accent: "cyan" as const,
    line: "Train and serve on whatever silicon you have — or none at all.",
    chips: ["Discrete GPU", "Integrated GPU", "Workstation", "Cloud GPU", "CPU-only", "Any vendor"],
    body:
      "Run inference on a desktop GPU, a workstation, a laptop, or a CPU-only board. OhhO Serve picks the execution provider that's present — no vendor-specific SDK is required to ship a policy.",
  },
  {
    title: "Any robot — open-source first",
    icon: Bot,
    accent: "violet" as const,
    line: "Built for open-source robots. Open to any brand with an open software layer.",
    chips: ["Open-source HW", "3D-printed", "ROS 2-native", "Open SDK brands", "Open firmware", "Any form factor"],
    body:
      "OhhO targets open-source robots and open-source robot hardware first — wheeled, legged, humanoid, arm, drone or marine. It also embraces commercial brands that publish an open SDK and welcome third-party developers: capability-typed commands mean swapping hardware never rewrites your application, and per-brand adapters bridge non-ROS robots in unchanged.",
  },
  {
    title: "Any model",
    icon: BrainCircuit,
    accent: "cyan" as const,
    line: "Bring your own policy. Swap it anytime. Keep your data.",
    chips: ["VLA", "Imitation BC", "Diffusion policy", "RL / ONNX", "Your own fine-tune", "Any backend"],
    body:
      "Serve is model-agnostic by design — a vision-language-action model, an imitation-learning policy, a diffusion policy, an ONNX RL export, or a checkpoint you fine-tuned yourself. Your models and your data stay yours.",
  },
  {
    title: "Any cloud — or none",
    icon: Cloud,
    accent: "violet" as const,
    line: "Self-host everything. Use OhhO Cloud only if you want to.",
    chips: ["Self-hosted", "Public cloud", "Private cloud", "On-prem", "Hybrid", "OhhO Cloud"],
    body:
      "Every console, driver and the engine itself is open source and runs on your own machines. OhhO Cloud is an option for managed GPUs and fleet ops — never a requirement. Move workloads anytime.",
  },
  {
    title: "Any transport",
    icon: Cable,
    accent: "cyan" as const,
    line: "Whatever wire your robot speaks, OhhO speaks it too.",
    chips: ["ROS 2", "DDS", "MAVLink", "Modbus", "Web Serial", "BLE"],
    body:
      "ROSBridge over Wi-Fi, Web Serial over USB, Web Bluetooth, DDS, MAVLink or Modbus — all behind one transport interface. A console built against OhhO works on any robot with an adapter.",
  },
  {
    title: "Open ecosystems welcome",
    icon: Unlock,
    accent: "violet" as const,
    line: "Closed software? Not here. Open layers and third-party devs, welcome.",
    chips: ["Open SDK", "Public APIs", "Third-party devs", "Open firmware", "No walled gardens", "MIT / Apache"],
    body:
      "OhhO works with any brand that keeps its software layer open source and welcomes outside developers into its ecosystem. Closed, walled-garden platforms aren't the target — but if a vendor opens up, OhhO's adapters plug them straight in. Apache-2.0 engine, MIT tools, standard LeRobot datasets and ONNX exports mean you can leave anytime and keep everything you made.",
  },
];

// The "what you're never locked into" strip — a compact, scannable restatement.
const NEVER_LOCKED = [
  "A GPU vendor",
  "A closed-software brand",
  "A walled garden",
  "A model lab",
  "A cloud provider",
  "A single supplier",
];

export default function BrandAgnostic() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="no-lock-in" style={{ padding: "112px 24px" }} className="relative">
      <div className="max-w-content mx-auto">
        {/* heading */}
        <div
          ref={headRef}
          className="text-center max-w-[720px] mx-auto mb-[56px] transition-all duration-[650ms]"
          style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
        >
          <div
            className="inline-flex items-center gap-2 font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px] px-[14px] py-[6px] rounded-full"
            style={{
              color: "var(--cyan)",
              background: "rgba(0,212,255,.07)",
              border: "1px solid rgba(0,212,255,.20)",
            }}
          >
            <Unlock size={11} strokeWidth={2.2} />
            Open-source robots · Open ecosystems
          </div>
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
            Open-source robots, first.<br />
            <span className="text-cyan">Open ecosystems, welcome.</span>
          </h2>
          <p className="text-[16px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            <span className="text-white font-semibold">OhhO targets open-source robotics.</span> Open-source
            robots and open-source robot hardware are the primary focus — and the platform also works with any
            commercial brand that keeps its software layer open source and welcomes third-party developers
            into its ecosystem. The best open-source robotics, for the cheapest price, under one umbrella — no
            vendor lock-in.
          </p>
        </div>

        {/* two-tier positioning callout — the core message, impossible to miss */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-[18px] mb-[40px] max-w-[820px] mx-auto">
          <GlassCard accent="cyan" featured padding="24px 22px" className="flex flex-col gap-2">
            <div className="font-mono text-[9.5px] font-semibold tracking-[0.14em] uppercase" style={{ color: "var(--cyan)" }}>
              Primary
            </div>
            <div className="font-display text-[17px] font-semibold legible">Open-source robots &amp; hardware</div>
            <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.66)" }}>
              Built first for open-source robots — 3D-printed humanoids, open quadrupeds, open arms, open
              drones. If the hardware designs and firmware are open, OhhO is the native home.
            </p>
          </GlassCard>
          <GlassCard accent="violet" padding="24px 22px" className="flex flex-col gap-2">
            <div className="font-mono text-[9.5px] font-semibold tracking-[0.14em] uppercase" style={{ color: "var(--violet-lite)" }}>
              Also
            </div>
            <div className="font-display text-[17px] font-semibold legible">Open-software brands welcome</div>
            <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.66)" }}>
              Any commercial brand that keeps its software layer open source and welcomes third-party
              developers into its ecosystem plugs in through OhhO&apos;s adapters. Closed, walled-garden
              platforms aren&apos;t the target — open up, and you&apos;re in.
            </p>
          </GlassCard>
        </div>

        {/* "Bring your own ___" grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[18px] mb-[52px]">
          {LAYERS.map((l, i) => {
            const Icon = l.icon;
            const isCyan = l.accent === "cyan";
            const aColor = isCyan ? "var(--cyan)" : "var(--violet-lite)";
            return (
              <GlassCard
                key={l.title}
                accent={isCyan ? "cyan" : "violet"}
                delay={[0, 0.08, 0.16][i % 3]}
                padding="26px"
                className="flex flex-col gap-[14px] h-full"
              >
                <div className="flex items-center gap-3">
                  <div
                    className="glass-pop w-[42px] h-[42px] rounded-[11px] flex items-center justify-center flex-shrink-0"
                    style={{
                      color: aColor,
                      background: isCyan ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)",
                      border: isCyan ? "1px solid rgba(0,212,255,.24)" : "1px solid rgba(124,58,237,.26)",
                    }}
                  >
                    <Icon size={19} strokeWidth={1.7} />
                  </div>
                  <div>
                    <div className="font-display text-[16px] font-semibold">{l.title}</div>
                    <div className="text-[12px] font-medium mt-[1px]" style={{ color: aColor }}>
                      {l.line}
                    </div>
                  </div>
                </div>

                <p className="text-[13px] leading-[1.65] flex-1" style={{ color: "rgba(255,255,255,0.66)" }}>
                  {l.body}
                </p>

                <div className="flex flex-wrap gap-1.5">
                  {l.chips.map((c) => (
                    <span
                      key={c}
                      className="font-mono text-[10.5px] px-2 py-[3px] rounded-full"
                      style={{
                        color: aColor,
                        background: isCyan ? "rgba(0,212,255,.08)" : "rgba(124,58,237,.10)",
                        border: isCyan ? "1px solid rgba(0,212,255,.20)" : "1px solid rgba(124,58,237,.22)",
                      }}
                    >
                      {c}
                    </span>
                  ))}
                </div>
              </GlassCard>
            );
          })}
        </div>

        {/* "What you're never locked into" strip */}
        <GlassCard interactive={false} padding="32px 28px" radius={22} className="overflow-hidden">
          <div className="flex flex-col md:flex-row items-center gap-6 md:gap-8">
            <div className="flex items-center gap-3 flex-shrink-0">
              <ShieldCheck size={22} strokeWidth={1.7} style={{ color: "var(--cyan)" }} />
              <div className="font-display text-[15px] font-semibold leading-tight">
                What you&apos;re<br className="hidden md:block" /> never locked into
              </div>
            </div>
            <div className="flex flex-wrap items-center justify-center gap-2.5 flex-1">
              {NEVER_LOCKED.map((n, i) => (
                <motion.span
                  key={n}
                  initial={{ opacity: 0, y: 8 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ duration: 0.4, delay: i * 0.05 }}
                  className="font-mono text-[12px] px-3 py-1.5 rounded-full inline-flex items-center gap-1.5"
                  style={{
                    color: "rgba(255,255,255,0.78)",
                    background: "rgba(255,255,255,0.04)",
                    border: "1px solid rgba(255,255,255,0.10)",
                  }}
                >
                  <span style={{ color: "var(--cyan)", fontSize: 13, lineHeight: 1 }}>×</span>
                  {n}
                </motion.span>
              ))}
            </div>
          </div>
        </GlassCard>

        {/* closing line + CTA */}
        <div className="text-center mt-[40px]">
          <p className="text-[14px] leading-[1.7] max-w-[560px] mx-auto mb-6" style={{ color: "rgba(255,255,255,0.55)" }}>
            Open-source robots in. Open formats out. Standard LeRobot datasets, ONNX exports, ROS 2 topics
            and Apache-2.0 source — so the work you do on OhhO is portable to anything, forever. Open
            software and open ecosystems aren&apos;t a feature here; they&apos;re the whole point.
          </p>
          <a
            href={GITHUB_HREF}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 text-sm font-medium px-[22px] py-[12px] rounded-lg transition-all duration-200"
            style={{
              background: "rgba(255,255,255,0.04)",
              color: "var(--text)",
              border: "1px solid rgba(255,255,255,0.16)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.3)";
              (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,.06)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.16)";
              (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,0.04)";
            }}
          >
            Read the open-source code <ArrowRight size={14} strokeWidth={2.5} />
          </a>
        </div>
      </div>
    </section>
  );
}
