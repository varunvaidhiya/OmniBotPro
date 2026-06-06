"use client";

import { motion } from "framer-motion";
import { ArrowRight, ChevronDown } from "lucide-react";

export default function Hero() {
  return (
    <section
      id="home"
      className="relative min-h-screen flex flex-col items-center justify-center text-center overflow-hidden"
      style={{ padding: "80px 24px 100px" }}
    >
      <div className="hero-grid" />
      <div className="hero-orb-1" />
      <div className="hero-orb-2" />
      <div className="hero-orb-3" />

      <div className="relative z-10 max-w-[820px] flex flex-col items-center">
        {/* Badge */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
          className="inline-flex items-center gap-2 font-mono text-[11px] tracking-[0.06em] mb-9 px-[14px] py-[6px] rounded-full"
          style={{
            color: "var(--cyan)",
            background: "var(--cyan-dim)",
            border: "1px solid rgba(0,212,255,.20)",
          }}
        >
          <span className="badge-dot" />
          ROS 2 Jazzy &nbsp;·&nbsp; Now in beta
        </motion.div>

        {/* Logo */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.1 }}
          className="relative inline-block mb-2"
        >
          <h1
            className="font-display font-bold leading-none flex items-center justify-center tracking-[-0.045em] group"
            style={{ fontSize: "clamp(88px, 17vw, 172px)" }}
          >
            <span
              className="text-cyan inline-block transition-[text-shadow] duration-400 group-hover:[text-shadow:0_0_40px_rgba(0,212,255,.65),0_0_80px_rgba(0,212,255,.28)]"
            >O</span>
            <span className="tracking-[-0.05em]">hh</span>
            <span
              className="text-cyan inline-block transition-[text-shadow] duration-400 group-hover:[text-shadow:0_0_40px_rgba(0,212,255,.65),0_0_80px_rgba(0,212,255,.28)]"
            >O</span>
          </h1>

          {/* Mirror reflection */}
          <div className="hero-logo-mirror" aria-hidden="true">
            <span className="text-cyan">O</span>
            <span style={{ letterSpacing: "-0.05em" }}>hh</span>
            <span className="text-cyan">O</span>
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.25 }}
          className="font-mono text-[12px] font-medium tracking-[0.14em] uppercase mb-[14px] mt-12"
          style={{ color: "var(--cyan)" }}
        >
          The Robotics Operating Platform
        </motion.div>

        <motion.p
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.35 }}
          className="font-display text-[clamp(16px,2.6vw,24px)] font-normal leading-[1.6] max-w-[600px] mx-auto mb-3"
          style={{ color: "rgba(255,255,255,0.52)" }}
        >
          From <strong className="text-white font-semibold">VR teleoperation</strong> to{" "}
          <strong className="text-white font-semibold">AI inference</strong> —
          one platform to build, deploy and scale any robot.
        </motion.p>

        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.5, delay: 0.45 }}
          className="font-mono text-[14px] tracking-[0.08em] mb-10"
          style={{ color: "rgba(255,255,255,0.22)" }}
        >
          Build it.&nbsp; Fly it.&nbsp; Scale it.
        </motion.p>

        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, delay: 0.55 }}
          className="flex items-center justify-center gap-[14px] flex-wrap"
        >
          <a
            href="#"
            className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5 hover:opacity-95"
            style={{
              background: "var(--cyan)",
              color: "var(--bg)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "0 10px 32px rgba(0,212,255,0.22)";
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
              background: "transparent",
              color: "var(--text)",
              border: "1px solid rgba(255,255,255,0.13)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.3)";
              (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,.04)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.13)";
              (e.currentTarget as HTMLElement).style.background = "transparent";
            }}
          >
            View Products
          </a>
        </motion.div>
      </div>

      <div className="scroll-hint">
        <ChevronDown size={14} strokeWidth={1.5} />
        scroll
      </div>
    </section>
  );
}
