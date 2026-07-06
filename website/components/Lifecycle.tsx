"use client";

/*
 * Lifecycle — the homepage "single umbrella" moat section.
 *
 * OhhO's second core promise: one umbrella covering the ENTIRE robot lifecycle,
 * end to end — from a blank-canvas prototype to a certified, regulated fleet.
 * No stitching together a dozen point tools from a dozen vendors; every stage
 * is a console on the same open engine, on the same bill, for the cheapest
 * price in robotics.
 *
 * Rendered after OhhoOS (the engine) and before Products (the catalog) so the
 * narrative reads: engine → "no lock-in" → "one umbrella, whole lifecycle" →
 * the nineteen products that fill each stage.
 */

import { motion } from "framer-motion";
import {
  PenTool,
  GraduationCap,
  Rocket,
  Send,
  LayoutGrid,
  Scale,
  ArrowRight,
} from "lucide-react";
import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";

const STAGES = [
  {
    n: "01",
    title: "Prototype",
    icon: PenTool,
    accent: "cyan" as const,
    body: "Design any robot in the browser, assemble it at the bench, and bring up a wired, calibrated, powered-on unit — starting from a blank canvas or an industry template.",
    products: ["Build", "Bench", "Frame"],
  },
  {
    n: "02",
    title: "Train",
    icon: GraduationCap,
    accent: "violet" as const,
    body: "Collect demonstrations, fine-tune VLA / imitation / RL policies, run sweeps and a continual-learning loop — then export a deployment-ready checkpoint or ONNX policy.",
    products: ["Data", "Train", "Twin"],
  },
  {
    n: "03",
    title: "Launch",
    icon: Rocket,
    accent: "cyan" as const,
    body: "Serve the model behind a REST endpoint, connect any robot over any transport, and teleoperate or run autonomy to put the robot to work — in sim first, then for real.",
    products: ["Serve", "Connect", "Pilot", "Autonomy"],
  },
  {
    n: "04",
    title: "Deploy",
    icon: Send,
    accent: "violet" as const,
    body: "Ship policies over the air to a fleet of any size, monitor every robot from a single pane, and keep drivers, firmware and models current across mixed hardware.",
    products: ["Fleet", "Bridge", "View"],
  },
  {
    n: "05",
    title: "Manage",
    icon: LayoutGrid,
    accent: "cyan" as const,
    body: "Run the whole fleet day to day — observe and diagnose every robot, buy and sell skills in a marketplace, and keep the AI agent on task across the operation.",
    products: ["Care", "Market", "Mind", "Link"],
  },
  {
    n: "06",
    title: "Regulate",
    icon: Scale,
    accent: "violet" as const,
    body: "Close the trust loop — prove every behavior is safe and reproducible, stay ahead of CVEs, and certify against the standards your industry and insurers require.",
    products: ["Proof", "Shield", "Comply"],
  },
];

export default function Lifecycle() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="lifecycle" style={{ padding: "112px 24px" }} className="relative">
      <div className="max-w-content mx-auto">
        {/* heading */}
        <div
          ref={headRef}
          className="text-center max-w-[720px] mx-auto mb-[64px] transition-all duration-[650ms]"
          style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
        >
          <div
            className="inline-flex items-center gap-2 font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px] px-[14px] py-[6px] rounded-full"
            style={{
              color: "var(--violet-lite)",
              background: "rgba(124,58,237,.08)",
              border: "1px solid rgba(124,58,237,.22)",
            }}
          >
            <LayoutGrid size={11} strokeWidth={2.2} />
            One umbrella · the whole lifecycle
          </div>
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
            From prototype to regulated fleet.<br />
            <span className="text-cyan">All under one roof.</span>
          </h2>
          <p className="text-[16px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            <span className="text-white font-semibold">Build, train, launch, deploy, manage and regulate</span>{" "}
            any robot — without stitching together a dozen tools from a dozen vendors. Every stage is a
            console on the same open engine, on one bill, for the cheapest price in robotics. That&apos;s the
            OhhO moat: pure value, end to end.
          </p>
        </div>

        {/* lifecycle flow */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[18px]">
          {STAGES.map((s, i) => {
            const Icon = s.icon;
            const isCyan = s.accent === "cyan";
            const aColor = isCyan ? "var(--cyan)" : "var(--violet-lite)";
            return (
              <motion.div
                key={s.n}
                initial={{ opacity: 0, y: 22 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{ duration: 0.55, delay: [0, 0.08, 0.16][i % 3] }}
              >
                <GlassCard
                  accent={isCyan ? "cyan" : "violet"}
                  padding="26px"
                  className="flex flex-col gap-[14px] h-full"
                >
                  {/* stage number + icon */}
                  <div className="flex items-center justify-between">
                    <div
                      className="glass-pop w-[44px] h-[44px] rounded-[12px] flex items-center justify-center"
                      style={{
                        color: aColor,
                        background: isCyan ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)",
                        border: isCyan ? "1px solid rgba(0,212,255,.24)" : "1px solid rgba(124,58,237,.26)",
                      }}
                    >
                      <Icon size={20} strokeWidth={1.7} />
                    </div>
                    <span
                      className="font-display font-bold text-[40px] leading-none tracking-tight"
                      style={{ color: isCyan ? "rgba(0,212,255,.16)" : "rgba(124,58,237,.20)" }}
                    >
                      {s.n}
                    </span>
                  </div>

                  <div className="font-display text-[18px] font-semibold">{s.title}</div>

                  <p className="text-[13px] leading-[1.65] flex-1" style={{ color: "rgba(255,255,255,0.66)" }}>
                    {s.body}
                  </p>

                  {/* product chips mapped to this stage */}
                  <div className="flex flex-wrap items-center gap-1.5 pt-1">
                    <span
                      className="font-mono text-[9px] tracking-[0.12em] uppercase mr-1"
                      style={{ color: "rgba(255,255,255,0.35)" }}
                    >
                      OhhO
                    </span>
                    {s.products.map((p) => (
                      <span
                        key={p}
                        className="font-mono text-[10.5px] px-2 py-[3px] rounded-full"
                        style={{
                          color: aColor,
                          background: isCyan ? "rgba(0,212,255,.08)" : "rgba(124,58,237,.10)",
                          border: isCyan ? "1px solid rgba(0,212,255,.20)" : "1px solid rgba(124,58,237,.22)",
                        }}
                      >
                        {p}
                      </span>
                    ))}
                  </div>
                </GlassCard>
              </motion.div>
            );
          })}
        </div>

        {/* closing strip — the moat in one line */}
        <div className="mt-[48px] text-center">
          <div
            className="inline-flex items-center gap-3 px-5 py-3 rounded-2xl text-[14px] flex-wrap justify-center max-w-[680px]"
            style={{
              background: "rgba(0,212,255,0.05)",
              border: "1px solid rgba(0,212,255,0.18)",
              color: "rgba(255,255,255,0.78)",
            }}
          >
            <span style={{ color: "var(--cyan)", fontSize: 16 }}>⊛</span>
            <span>
              <strong className="text-white">One platform. One bill. Any robot, any brand, any GPU.</strong>{" "}
              The cheapest path from idea to certified fleet — without owing anyone your stack.
            </span>
          </div>
          <div className="mt-7">
            <a
              href="#products"
              className="inline-flex items-center gap-2 text-sm font-semibold"
              style={{ color: "var(--cyan)" }}
            >
              See the 19 consoles that cover it all <ArrowRight size={14} strokeWidth={2.5} />
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
