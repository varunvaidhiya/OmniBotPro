"use client";

/*
 * OhhO OS — the homepage "umbrella" section.
 *
 * Positions OhhO OS as the single open-source engine that sits ON TOP of (and
 * powers) all nineteen products. Rendered between StatsBar and Products on the
 * home page so the narrative reads: hero → proof → "one engine" → the products
 * that ride on it. Differentiation copy is deliberately competitor-agnostic —
 * it states OhhO OS's advantages without naming any other platform.
 */

import Link from "next/link";
import { ArrowRight, Network, GitBranch, Layers, Cpu } from "lucide-react";
import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";
import { OS_HREF, DOCS_HREF } from "@/lib/site";

const DIFFERENTIATORS = [
  {
    title: "ROS optional — never required",
    body:
      "Most agentic robot stacks force a choice: go all-in on ROS, or abandon it entirely. OhhO OS runs both ways — a lightweight pure-Python runtime or the full ROS 2 stack — and you switch with a single argument. get_runtime(\"auto\") detects ROS 2 automatically.",
    icon: Network,
  },
  {
    title: "Switch robots without rewriting",
    body:
      "Write a behavior once and run it on a wheeled base, a quadruped, a humanoid or an arm. Capability-typed commands mean swapping hardware never means rewriting your application.",
    icon: GitBranch,
  },
  {
    title: "Agent · Train · Serve — built in",
    body:
      "A real perceive→reason→act→reflect agent brain, record→train→serve training pipeline, and a skill market — all from the same pip install. Mock mode for sim loops, device=\"auto\" everywhere.",
    icon: Layers,
  },
  {
    title: "Open source, zero lock-in",
    body:
      "Apache-2.0 licensed. 151 tests passing. Self-host every line, bring your own models and data, and move to OhhO Cloud only when you want managed GPUs, training and fleet operations.",
    icon: Cpu,
  },
];

const PRODUCT_CHIPS = ["Build", "Frame", "Serve", "Train", "Autonomy", "Mind", "Fleet", "+12 more"];
const ROBOT_CHIPS = ["Wheeled", "Legged", "Humanoid", "Arm", "Drone", "+10 more"];

export default function OhhoOS() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="ohho-os" style={{ padding: "112px 24px" }} className="relative">
      <div className="max-w-content mx-auto">
        {/* heading */}
        <div
          ref={headRef}
          className="text-center max-w-[680px] mx-auto mb-[56px] transition-all duration-[650ms]"
          style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
        >
          <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--cyan)" }}>
            The engine
          </div>
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
            One open engine under<br />every product.
          </h2>
          <p className="text-[16px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            <span className="text-white font-semibold">OhhO OS</span> is the open-source robot engine the
            entire platform runs on — a single, robot-agnostic runtime that controls any robot, with or
            without ROS, and carries everything from perception to training. Every OhhO product is a
            console on top of it.
          </p>
        </div>

        {/* umbrella stack visual: OS → products → robots */}
        <div className="flex flex-col items-center mb-[64px]">
          <div className="w-full max-w-[440px]">
            <GlassCard accent="cyan" featured padding="22px 26px" className="text-center">
              <div className="font-display text-[20px] font-bold mb-1">
                <span className="text-cyan">OhhO</span> OS
              </div>
              <div className="font-mono text-[11px] tracking-[0.04em]" style={{ color: "rgba(255,255,255,0.55)" }}>
                v1.0.0 · Python · ROS 2 · No-ROS · Agent · Train · Serve · Skills
              </div>
            </GlassCard>
          </div>

          <Connector label="powers" />
          <ChipRow label="19 products" chips={PRODUCT_CHIPS} accent="cyan" />
          <Connector label="control" />
          <ChipRow label="any robot" chips={ROBOT_CHIPS} accent="violet" />
        </div>

        {/* differentiators (no competitor names) */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-[18px] mb-[52px]">
          {DIFFERENTIATORS.map((d, i) => {
            const Icon = d.icon;
            const isCyan = i % 2 === 0;
            return (
              <GlassCard
                key={d.title}
                accent={isCyan ? "cyan" : "violet"}
                delay={isCyan ? 0 : 0.08}
                padding="26px"
                className="flex gap-4"
              >
                <div
                  className="glass-pop w-[42px] h-[42px] rounded-[11px] flex items-center justify-center flex-shrink-0"
                  style={{
                    color: isCyan ? "var(--cyan)" : "var(--violet)",
                    background: isCyan ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)",
                    border: isCyan ? "1px solid rgba(0,212,255,.24)" : "1px solid rgba(124,58,237,.26)",
                  }}
                >
                  <Icon size={19} strokeWidth={1.7} />
                </div>
                <div>
                  <div className="font-display text-[16px] font-semibold mb-1.5">{d.title}</div>
                  <p className="text-[13.5px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.66)" }}>
                    {d.body}
                  </p>
                </div>
              </GlassCard>
            );
          })}
        </div>

        {/* install snippet + CTAs */}
        <div className="flex flex-col items-center gap-6">
          <div
            className="w-full max-w-[520px] rounded-xl p-4 font-mono text-[12.5px] leading-[1.9]"
            style={{ background: "rgba(0,0,0,0.4)", border: "1px solid rgba(255,255,255,0.10)", color: "rgba(255,255,255,0.8)" }}
          >
            <div>
              <span style={{ color: "var(--cyan)" }}>$</span> pip install{" "}
              <span style={{ color: "var(--cyan)" }}>&apos;ohho-os[base]&apos;</span>
            </div>
            <div style={{ color: "rgba(255,255,255,0.45)" }}>&gt;&gt;&gt; from ohho import Robot</div>
            <div style={{ color: "rgba(255,255,255,0.45)" }}>
              &gt;&gt;&gt; bot = Robot.connect(<span style={{ color: "#9ae6b4" }}>&quot;omnibot&quot;</span>){" "}
              <span style={{ color: "rgba(255,255,255,0.3)" }}># any robot · ROS or no-ROS</span>
            </div>
          </div>

          <div className="flex items-center gap-[14px] flex-wrap justify-center">
            <Link
              href={`${DOCS_HREF}/ohho-os/setup`}
              className="inline-flex items-center gap-2 text-sm font-semibold px-[24px] py-[12px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Setup Guide <ArrowRight size={15} strokeWidth={2.5} />
            </Link>
            <Link
              href={OS_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[24px] py-[12px] rounded-lg transition-all duration-200 hover:opacity-90"
              style={{ background: "rgba(255,255,255,0.04)", color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Explore OhhO OS
            </Link>
            <Link
              href={`${DOCS_HREF}/ohho-os`}
              className="inline-flex items-center gap-2 text-sm font-medium px-[24px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.55)" }}
            >
              Read the docs
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}

function Connector({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center py-2.5" aria-hidden>
      <div className="w-px h-5" style={{ background: "linear-gradient(to bottom, rgba(0,212,255,0.5), rgba(0,212,255,0))" }} />
      <span className="font-mono text-[9px] tracking-[0.14em] uppercase my-1" style={{ color: "rgba(255,255,255,0.32)" }}>
        {label}
      </span>
      <div className="w-px h-5" style={{ background: "linear-gradient(to bottom, rgba(0,212,255,0), rgba(0,212,255,0.5))" }} />
    </div>
  );
}

function ChipRow({ label, chips, accent }: { label: string; chips: string[]; accent: "cyan" | "violet" }) {
  const color = accent === "cyan" ? "var(--cyan)" : "var(--violet)";
  const bg = accent === "cyan" ? "rgba(0,212,255,.08)" : "rgba(124,58,237,.10)";
  const border = accent === "cyan" ? "rgba(0,212,255,.22)" : "rgba(124,58,237,.24)";
  return (
    <div
      className="w-full max-w-[560px] rounded-2xl p-4"
      style={{ background: "rgba(255,255,255,0.02)", border: "1px solid rgba(255,255,255,0.08)" }}
    >
      <div className="font-mono text-[9px] tracking-[0.14em] uppercase text-center mb-3" style={{ color: "rgba(255,255,255,0.4)" }}>
        {label}
      </div>
      <div className="flex flex-wrap items-center justify-center gap-2">
        {chips.map((c) => (
          <span
            key={c}
            className="font-mono text-[11px] px-2.5 py-1 rounded-full"
            style={{ color, background: bg, border: `1px solid ${border}` }}
          >
            {c}
          </span>
        ))}
      </div>
    </div>
  );
}
