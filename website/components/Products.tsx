"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
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
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4">
              Everything you need to<br />build and operate robots.
            </h2>
            <p className="text-[16px] leading-[1.7] max-w-[520px]" style={{ color: "rgba(255,255,255,0.52)" }}>
              Six purpose-built products. One unified platform. Works with any ROS 2 compatible hardware.
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[18px]">
          {products.map((p, i) => (
            <ProductCard key={p.name} product={p} delay={[0, 0.08, 0.16, 0.08, 0.16, 0.24][i]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function ProductCard({ product, delay }: { product: typeof products[0]; delay: number }) {
  const { ref, inView } = useScrollReveal();
  const isCyan = product.accent === "cyan";

  return (
    <div
      ref={ref}
      className="pc-card group flex flex-col gap-[14px] p-7 rounded-[14px] cursor-default transition-all duration-[250ms]"
      style={{
        background: "var(--surf)",
        border: "1px solid rgba(255,255,255,0.07)",
        opacity: inView ? 1 : 0,
        transform: inView ? "translateY(0)" : "translateY(22px)",
        transition: "opacity 0.65s ease, transform 0.65s ease, border-color 0.25s, box-shadow 0.25s",
        transitionDelay: `${delay}s`,
      }}
      onMouseEnter={(e) => {
        const el = e.currentTarget as HTMLElement;
        if (isCyan) {
          el.style.borderColor = "rgba(0,212,255,.22)";
          el.style.boxShadow = "0 0 0 1px rgba(0,212,255,.06) inset, 0 12px 36px rgba(0,0,0,.5)";
        } else {
          el.style.borderColor = "rgba(124,58,237,.28)";
          el.style.boxShadow = "0 0 0 1px rgba(124,58,237,.06) inset, 0 12px 36px rgba(0,0,0,.5)";
        }
        el.style.transform = "translateY(-3px)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget as HTMLElement;
        el.style.borderColor = "rgba(255,255,255,0.07)";
        el.style.boxShadow = "";
        el.style.transform = inView ? "translateY(0)" : "translateY(22px)";
      }}
    >
      <div
        className="w-[42px] h-[42px] rounded-[10px] flex items-center justify-center"
        style={{ background: isCyan ? "rgba(0,212,255,.10)" : "rgba(124,58,237,.12)" }}
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

      <p className="text-[13px] leading-[1.65] flex-1" style={{ color: "rgba(255,255,255,0.52)" }}>
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
    </div>
  );
}
