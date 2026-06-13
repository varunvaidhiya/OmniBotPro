"use client";

import { motion } from "framer-motion";
import { ArrowRight, ChevronDown } from "lucide-react";
import dynamic from "next/dynamic";

// react-three-fiber must only run in the browser (static export friendly)
const RobotScene = dynamic(() => import("./robot/RobotScene"), {
  ssr: false,
  loading: () => null,
});

export default function Hero() {
  return (
    <section
      id="home"
      className="relative min-h-screen flex flex-col overflow-hidden"
    >
      {/* colour bloom behind the 3D scene */}
      <div className="hero-orb-1" />
      <div className="hero-orb-2" />
      <div className="hero-orb-3" />

      {/* ── interactive 3D OmniBot — fills the hero, receives mouse/drag ── */}
      <div className="absolute inset-0 z-[5]">
        <RobotScene />
      </div>

      {/* legibility gradients top & bottom */}
      <div className="hero-fade-top" />
      <div className="hero-fade-bottom" />

      {/* ── overlay content (pointer-events pass through to the canvas) ── */}
      <div className="relative z-10 flex flex-col min-h-screen pointer-events-none px-6 pt-28 pb-10">
        {/* TOP — badge, wordmark, tagline */}
        <div className="flex flex-col items-center text-center">
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className="inline-flex items-center gap-2 font-mono text-[11px] tracking-[0.06em] mb-6 px-[14px] py-[6px] rounded-full"
            style={{
              color: "var(--cyan)",
              background: "rgba(0,212,255,.07)",
              border: "1px solid rgba(0,212,255,.20)",
              backdropFilter: "blur(8px)",
            }}
          >
            <span className="badge-dot" />
            ROS 2 Jazzy &nbsp;·&nbsp; Mecanum + SmolVLA &nbsp;·&nbsp; Now in beta
          </motion.div>

          <motion.h1
            initial={{ opacity: 0, y: 22 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.1 }}
            className="font-display font-bold leading-none flex items-center justify-center tracking-[-0.045em] group mb-4"
            style={{ fontSize: "clamp(64px, 12vw, 132px)" }}
          >
            <span className="text-cyan inline-block transition-[text-shadow] duration-300 group-hover:[text-shadow:0_0_40px_rgba(0,212,255,.65),0_0_80px_rgba(0,212,255,.28)]">
              O
            </span>
            <span className="tracking-[-0.05em]">hh</span>
            <span className="text-cyan inline-block transition-[text-shadow] duration-300 group-hover:[text-shadow:0_0_40px_rgba(0,212,255,.65),0_0_80px_rgba(0,212,255,.28)]">
              O
            </span>
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.25 }}
            className="font-mono text-[12px] font-medium tracking-[0.14em] uppercase"
            style={{ color: "var(--cyan)" }}
          >
            The Robotics Operating Platform
          </motion.div>
        </div>

        {/* spacer keeps the robot centred between the two text blocks */}
        <div className="flex-1" />

        {/* BOTTOM — value prop, CTAs, live control hint */}
        <div className="flex flex-col items-center text-center">
          <motion.p
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.35 }}
            className="font-display text-[clamp(15px,2.2vw,20px)] font-normal leading-[1.55] max-w-[560px] mx-auto mb-6"
            style={{ color: "rgba(255,255,255,0.66)" }}
          >
            From <strong className="text-white font-semibold">VR teleoperation</strong> to{" "}
            <strong className="text-white font-semibold">AI inference</strong> — one platform to
            build, deploy and scale any robot.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, delay: 0.5 }}
            className="flex items-center justify-center gap-[14px] flex-wrap mb-7 pointer-events-auto"
          >
            <a
              href="#products"
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5 hover:opacity-95"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLElement).style.boxShadow =
                  "0 10px 32px rgba(0,212,255,0.30)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLElement).style.boxShadow = "";
              }}
            >
              Start Free <ArrowRight size={15} strokeWidth={2.5} />
            </a>
            <a
              href="#products"
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{
                background: "rgba(10,14,26,.4)",
                color: "var(--text)",
                border: "1px solid rgba(255,255,255,0.16)",
                backdropFilter: "blur(8px)",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.3)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.16)";
              }}
            >
              View Products
            </a>
          </motion.div>

          {/* live-control hint */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.6, delay: 0.7 }}
            className="flex items-center gap-x-5 gap-y-2 flex-wrap justify-center font-mono text-[11px] tracking-[0.04em]"
            style={{ color: "rgba(255,255,255,0.40)" }}
          >
            <span className="inline-flex items-center gap-1.5">
              <kbd className="hero-kbd">↑</kbd>
              <kbd className="hero-kbd">↓</kbd>
              <kbd className="hero-kbd">←</kbd>
              <kbd className="hero-kbd">→</kbd>
              drive
            </span>
            <span className="hidden sm:inline opacity-40">·</span>
            <span>move mouse to aim the arm</span>
            <span className="hidden sm:inline opacity-40">·</span>
            <span>drag to orbit</span>
          </motion.div>
        </div>
      </div>

      <div className="scroll-hint z-10">
        <ChevronDown size={14} strokeWidth={1.5} />
        scroll
      </div>
    </section>
  );
}
