"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";
import { ArrowRight } from "lucide-react";

const products = [
  {
    name: "OhhO Pilot",
    tag: "Operate any robot. From anywhere.",
    desc: "VR and mobile app for real-time robot teleoperation. Pre-loaded robot profiles, custom robot builder, arm IK via hand tracking.",
    accent: "cyan",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="6" width="20" height="12" rx="3"/>
        <path d="M12 10v4"/><path d="M10 12h4"/>
        <circle cx="17" cy="12" r="1" fill="var(--cyan)" stroke="none"/>
        <circle cx="19" cy="10" r="1" fill="var(--cyan)" stroke="none"/>
      </svg>
    ),
  },
  {
    name: "OhhO Serve",
    tag: "Robot AI inference, as an API.",
    desc: "Deploy Vision-Language-Action models for your robot in one command. REST API, pluggable model backends, Prometheus metrics.",
    accent: "violet",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--violet-lite)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M13 2 3 14h9l-1 8 10-12h-9l1-8z"/>
      </svg>
    ),
  },
  {
    name: "OhhO Frame",
    tag: "Your robot stack, ready in one afternoon.",
    desc: "Boilerplate platform for building any robot. Docker, ROS 2, simulation, CI/CD — all pre-wired. Single or multi-machine deployments.",
    accent: "cyan",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M4 4h4v4H4z"/><path d="M16 4h4v4h-4z"/>
        <path d="M4 16h4v4H4z"/><path d="M16 16h4v4h-4z"/>
        <path d="M8 6h8"/><path d="M6 8v8"/><path d="M18 8v8"/><path d="M8 18h8"/>
      </svg>
    ),
  },
  {
    name: "OhhO Data",
    tag: "Collect. Label. Ship.",
    desc: "End-to-end pipeline for robot demonstration data collection. LeRobot-compatible dataset format, CLI tools, episode viewer.",
    accent: "violet",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--violet-lite)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <ellipse cx="12" cy="5" rx="9" ry="3"/>
        <path d="M3 5v14a9 3 0 0 0 18 0V5"/>
        <path d="M3 12a9 3 0 0 0 18 0"/>
      </svg>
    ),
  },
  {
    name: "OhhO View",
    tag: "Four cameras. One smart view.",
    desc: "Multi-camera bird's-eye-view perception for any robot. CPU-only, calibration UI included, ROS 2 ready.",
    accent: "cyan",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
        <circle cx="12" cy="12" r="3"/>
      </svg>
    ),
  },
  {
    name: "OhhO Fleet",
    tag: "Update 50 robots like you update an app.",
    desc: "Fleet management, OTA updates, observability stack. Prometheus, Grafana, AlertManager — all pre-configured.",
    accent: "violet",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--violet-lite)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="16" y="16" width="6" height="6" rx="1"/>
        <rect x="2" y="16" width="6" height="6" rx="1"/>
        <rect x="9" y="2" width="6" height="6" rx="1"/>
        <path d="M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3"/>
        <path d="M12 12V8"/>
      </svg>
    ),
  },
  {
    name: "OhhO Build",
    tag: "Design any robot. For any industry.",
    desc: "Browser-based 3-D robot designer. Pick components, identify required materials, source from suppliers — then bring your design to life with the full OhhO stack.",
    accent: "cyan",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="18" height="18" rx="2"/>
        <path d="M3 9h18"/>
        <path d="M9 21V9"/>
        <circle cx="8" cy="5.5" r=".8" fill="var(--cyan)" stroke="none"/>
        <circle cx="11.5" cy="5.5" r=".8" fill="var(--cyan)" stroke="none"/>
        <path d="m15 15 2.5 2.5"/>
        <path d="m17.5 15-2.5 2.5"/>
      </svg>
    ),
  },
];

export default function Products() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="products" style={{ padding: "112px 24px" }}>
      <div className="max-w-content mx-auto">
        <div className="mb-[60px]">
          <div
            ref={headRef}
            className="transition-all duration-[650ms]"
            style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
          >
            <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--cyan)" }}>
              Platform
            </div>
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
              Everything you need to<br />build and operate robots.
            </h2>
            <p className="text-[16px] leading-[1.7] max-w-[520px] legible" style={{ color: "rgba(255,255,255,0.62)" }}>
              Seven purpose-built products. One unified platform. Works with any ROS 2 compatible hardware.
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[18px]">
          {products.map((p, i) => (
            <ProductCard key={p.name} product={p} delay={[0, 0.08, 0.16, 0.08, 0.16, 0.24, 0.32][i]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function ProductCard({ product, delay }: { product: typeof products[0]; delay: number }) {
  const isCyan = product.accent === "cyan";

  return (
    <GlassCard
      accent={isCyan ? "cyan" : "violet"}
      delay={delay}
      padding="28px"
      className="pc-card group flex flex-col gap-[14px] cursor-default"
    >
      <div
        className="glass-pop w-[44px] h-[44px] rounded-[12px] flex items-center justify-center"
        style={{
          background: isCyan ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)",
          border: isCyan ? "1px solid rgba(0,212,255,.24)" : "1px solid rgba(124,58,237,.26)",
          boxShadow: isCyan
            ? "inset 0 0 22px rgba(0,212,255,.18)"
            : "inset 0 0 22px rgba(124,58,237,.20)",
        }}
      >
        <svg className="w-[19px] h-[19px]">{product.icon.props.children}</svg>
      </div>

      <div>
        <div className="font-display text-[17px] font-semibold">{product.name}</div>
        <div
          className="text-[12px] font-medium mt-[1px]"
          style={{ color: isCyan ? "var(--cyan)" : "var(--violet-lite)" }}
        >
          {product.tag}
        </div>
      </div>

      <p className="text-[13px] leading-[1.65] flex-1" style={{ color: "rgba(255,255,255,0.66)" }}>
        {product.desc}
      </p>

      <a
        href="#"
        className="inline-flex items-center gap-[5px] text-[12px] font-semibold font-mono tracking-[0.02em]"
        style={{ color: isCyan ? "var(--cyan)" : "var(--violet-lite)" }}
      >
        Learn more{" "}
        <ArrowRight size={12} strokeWidth={2.5} className="pc-link-arrow" />
      </a>
    </GlassCard>
  );
}
