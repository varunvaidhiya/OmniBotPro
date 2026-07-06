"use client";

/*
 * BrandAgnostic — the homepage "no vendor lock-in" moat section.
 *
 * This is the single most important marketing claim on the site: OhhO is a
 * brand-agnostic robotic platform. No dependency on a single supplier, GPU
 * vendor, robot brand, model lab, cloud, or transport. The customer keeps
 * pure value — the best open-source robotics, for the cheapest price, under
 * one umbrella — and is never locked into any one layer of the stack.
 *
 * Rendered between StatsBar and OhhoOS so the narrative reads:
 *   hero → proof → "no lock-in, ever" → "one open engine" → products.
 *
 * Copy is deliberately brand-naming-agnostic in the negative sense: it lists
 * the categories (GPU, robot, model, cloud, transport, brand) and gives a few
 * representative names per category to make "any" concrete — without ever
 * implying a partnership or dependency on any of them.
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
    title: "Any robot",
    icon: Bot,
    accent: "violet" as const,
    line: "One platform for every brand and every form factor.",
    chips: ["Wheeled", "Legged", "Humanoid", "Arm", "Drone", "Marine"],
    body:
      "Wheeled, legged, humanoid, arm, drone or marine — capability-typed commands mean swapping hardware never rewrites your application. Per-brand protocol adapters bridge non-ROS robots in unchanged.",
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
    title: "Any brand — no lock-in",
    icon: Unlock,
    accent: "violet" as const,
    line: "No single supplier, ecosystem or walled garden. Ever.",
    chips: ["MIT / Apache", "Open data formats", "Portable checkpoints", "No forced SDK"],
    body:
      "Apache-2.0 engine, MIT-licensed tools, standard LeRobot datasets and ONNX exports. Nothing in OhhO is built to trap you — it's built so you can leave anytime and still keep everything you made.",
  },
];

// The "what you're never locked into" strip — a compact, scannable restatement.
const NEVER_LOCKED = [
  "A GPU vendor",
  "A robot brand",
  "A model lab",
  "A cloud provider",
  "A transport protocol",
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
            Brand-agnostic · No vendor lock-in
          </div>
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
            No vendor lock-in.<br />
            <span className="text-cyan">Bring your own everything.</span>
          </h2>
          <p className="text-[16px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            <span className="text-white font-semibold">OhhO is a brand-agnostic robotic platform.</span>{" "}
            We reduce your dependency on any single brand, supplier, GPU vendor, robot maker, model lab,
            cloud or transport. You get the best open-source robotics — for the cheapest price — under one
            umbrella, and you keep the freedom to swap any layer at any time.
          </p>
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
            Open formats in. Open formats out. Standard LeRobot datasets, ONNX exports, ROS 2 topics and
            Apache-2.0 source — so the work you do on OhhO is portable to anything, forever.
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
