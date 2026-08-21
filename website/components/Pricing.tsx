"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";
import { Check } from "lucide-react";
import { contactMailto, SIGNUP_HREF, UPGRADE_HREF } from "@/lib/site";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

const plans = [
  {
    name: "Free",
    hl: "Sign in to get started",
    price: "Free",
    period: "Forever",
    features: [
      "All 19 OhhO product consoles",
      "OhhO Garage — robot setup & config",
      "OhhO Frame, Build, View, Proof",
      "OhhO Pilot, Fleet, Bridge, Market",
      "OhhO Comply, Shield, Bench, Care",
      "OhhO Autonomy, Data, Serve, Train",
      "OhhO Mind, Link, Twin consoles",
      "Local robot connection (Wi-Fi, USB, BLE)",
      "Community support",
    ],
    cta: "Sign In Free",
    ctaStyle: "outline",
    href: SIGNUP_HREF,
  },
  {
    name: "Builder",
    hl: "For indie devs and researchers",
    price: "$49",
    period: "per month",
    badge: "MOST POPULAR",
    features: [
      "Everything in Free",
      "OhhO Train — GPU cloud training (20 hrs/mo)",
      "OhhO Serve — AI inference API (500 calls/day)",
      "OhhO Mind — Cloud AI agent (LLM-backed)",
      "OhhO Link — MCP server + API key",
      "OhhO Twin — Cloud simulation & what-if",
      "OhhO Data — Cloud sync (1K episodes)",
      "Email support",
    ],
    cta: "Start Building",
    ctaStyle: "cyan",
    variant: "pop",
    href: UPGRADE_HREF,
  },
  {
    name: "Fleet",
    hl: "For teams running real hardware",
    price: "$199",
    period: "per month",
    features: [
      "Everything in Builder",
      "OhhO Train — 200 GPU hrs/mo",
      "OhhO Serve — 10K API calls/day",
      "OhhO Mind — 20K calls/mo + team memory",
      "OhhO Fleet — OTA updates, up to 100 robots",
      "OhhO Data — Unlimited + annotation",
      "OhhO Comply + Shield — compliance & CVE watch",
      "Priority support + Slack channel",
    ],
    cta: "Deploy Your Fleet",
    ctaStyle: "violet",
    variant: "fleet",
    href: UPGRADE_HREF,
  },
  {
    name: "Forge",
    hl: "Enterprise / white-label",
    price: "Custom",
    period: "Contact for pricing",
    features: [
      "Everything in Fleet",
      "Unlimited GPU & API usage",
      "On-prem OhhO Serve license",
      "Custom robot integrations",
      "OhhO Comply — custom standards + cert partner",
      "OhhO Shield — secure boot + SSO",
      "Private marketplace + white-label",
      "Dedicated SLA + onboarding",
      "White-label OhhO Pilot",
    ],
    cta: "Talk to Us",
    ctaStyle: "outline",
    href: contactMailto("OhhO Forge plan"),
  },
];

export default function Pricing() {
  const { ref: headRef, inView: headIn } = useScrollReveal();
  const { user, subscription } = useAuth();

  // Never show pricing to a subscribed user — anywhere.
  if (user && hasConsoleAccess(subscription)) return null;

  return (
    <section id="pricing" style={{ padding: "112px 24px" }}>
      <div className="max-w-content mx-auto">
        <div
          ref={headRef}
          className="text-center mb-[60px] transition-all duration-[650ms]"
          style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
        >
          <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--violet-lite)" }}>
            Pricing
          </div>
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
            Open source platform.<br />Pay only for cloud infrastructure.
          </h2>
          <p className="text-[16px] leading-[1.7] max-w-[560px] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            Every console, every tool, every driver — open source and free to use. Upgrade only when you need OhhO to run the infrastructure: GPU training, AI inference, MCP hosting, or cloud simulation.
          </p>

          {/* Open-source callout */}
          <div
            className="inline-flex items-center gap-3 mt-7 mb-2 px-5 py-3 rounded-2xl text-[13px]"
            style={{
              background: "rgba(0,212,255,0.06)",
              border: "1px solid rgba(0,212,255,0.22)",
              color: "rgba(255,255,255,0.75)",
            }}
          >
            <span style={{ color: "var(--cyan)", fontSize: 18 }}>⊛</span>
            <span>
              <strong className="text-white">MIT / Apache licensed.</strong>{" "}
              Self-host everything for free, or let OhhO run it — your choice.
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-[18px] items-start">
          {plans.map((plan, i) => (
            <PricingCard key={plan.name} plan={plan} delay={[0, 0.08, 0.16, 0.24][i]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function PricingCard({ plan, delay }: { plan: typeof plans[0]; delay: number }) {
  const isPop = plan.variant === "pop";
  const isFleet = plan.variant === "fleet";
  const isViolet = isFleet;
  const accent: "none" | "cyan" | "violet" = isPop ? "cyan" : isFleet ? "violet" : "none";

  return (
    <GlassCard
      accent={accent}
      featured={isPop || isFleet}
      delay={delay}
      padding="26px 22px"
      className="relative flex flex-col gap-[22px]"
    >
      {plan.badge && (
        <div
          className="absolute -top-[11px] left-1/2 -translate-x-1/2 font-mono text-[9px] font-semibold px-3 py-[3px] rounded-full whitespace-nowrap tracking-[0.07em]"
          style={{ background: "var(--cyan)", color: "var(--bg)", boxShadow: "0 4px 16px rgba(0,212,255,.4)" }}
        >
          {plan.badge}
        </div>
      )}

      <div>
        <div className="font-display text-[19px] font-bold">{plan.name}</div>
        <div className="text-[11px] mt-[3px]" style={{ color: "rgba(255,255,255,0.6)" }}>{plan.hl}</div>
      </div>

      <div className="glass-pop">
        <div
          className="font-display font-bold tracking-[-0.03em] leading-none"
          style={{ fontSize: plan.price === "Custom" ? "30px" : "44px", marginTop: plan.price === "Custom" ? "6px" : 0 }}
        >
          {plan.price !== "Free" && plan.price !== "Custom" && (
            <sup className="text-[18px] font-medium align-super leading-[2.2]" style={{ color: "rgba(255,255,255,0.6)" }}>$</sup>
          )}
          {plan.price === "Free" || plan.price === "Custom" ? plan.price : plan.price.replace("$", "")}
        </div>
        <div className="text-[12px] mt-[5px]" style={{ color: "rgba(255,255,255,0.6)" }}>{plan.period}</div>
      </div>

      <div className="flex flex-col gap-[9px] flex-1">
        {plan.features.map((f) => (
          <div key={f} className="flex items-start gap-[10px] text-[12px] leading-[1.55]" style={{ color: "rgba(255,255,255,0.66)" }}>
            <span
              className="w-[15px] h-[15px] rounded-full flex-shrink-0 mt-[1px] flex items-center justify-center"
              style={{
                background: isViolet ? "rgba(124,58,237,.10)" : "rgba(0,212,255,.10)",
                border: isViolet ? "1px solid rgba(124,58,237,.28)" : "1px solid rgba(0,212,255,.22)",
              }}
            >
              <Check
                size={8}
                strokeWidth={3}
                style={{ color: isViolet ? "var(--violet-lite)" : "var(--cyan)" }}
              />
            </span>
            {f}
          </div>
        ))}
      </div>

      <CtaButton style={plan.ctaStyle as string} label={plan.cta} href={plan.href} />
    </GlassCard>
  );
}

function CtaButton({ style, label, href }: { style: string; label: string; href: string }) {
  const base = "block text-center py-[11px] rounded-lg text-[13px] font-semibold cursor-pointer transition-all duration-200";

  if (style === "cyan") {
    return (
      <a
        href={href}
        className={base}
        style={{ background: "var(--cyan)", color: "var(--bg)" }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLElement).style.opacity = "0.88";
          (e.currentTarget as HTMLElement).style.boxShadow = "0 6px 24px rgba(0,212,255,0.22)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLElement).style.opacity = "1";
          (e.currentTarget as HTMLElement).style.boxShadow = "";
        }}
      >
        {label}
      </a>
    );
  }
  if (style === "violet") {
    return (
      <a
        href={href}
        className={base}
        style={{ background: "var(--violet)", color: "#fff" }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLElement).style.opacity = "0.88";
          (e.currentTarget as HTMLElement).style.boxShadow = "0 6px 24px rgba(124,58,237,0.22)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLElement).style.opacity = "1";
          (e.currentTarget as HTMLElement).style.boxShadow = "";
        }}
      >
        {label}
      </a>
    );
  }
  return (
    <a
      href={href}
      className={base}
      style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.34)";
        (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,.06)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.18)";
        (e.currentTarget as HTMLElement).style.background = "";
      }}
    >
      {label}
    </a>
  );
}
