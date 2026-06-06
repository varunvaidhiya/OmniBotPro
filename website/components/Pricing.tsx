"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import { Check } from "lucide-react";

const plans = [
  {
    name: "Spark",
    hl: "Good for hobbyists",
    price: "Free",
    period: "Forever",
    features: [
      "OhhO Frame (simulation only)",
      "OhhO View (open source)",
      "1 robot, local only",
      "Community support",
    ],
    cta: "Get Started Free",
    ctaStyle: "outline",
  },
  {
    name: "Builder",
    hl: "For indie devs and researchers",
    price: "$49",
    period: "per month",
    badge: "MOST POPULAR",
    features: [
      "Everything in Spark",
      "OhhO Pilot (mobile, up to 3 robots)",
      "OhhO Data (cloud sync, 1K episodes)",
      "OhhO Serve (500 API calls/day)",
      "Email support",
    ],
    cta: "Start Building",
    ctaStyle: "cyan",
    variant: "pop",
  },
  {
    name: "Fleet",
    hl: "For teams running real hardware",
    price: "$199",
    period: "per month",
    features: [
      "Everything in Builder",
      "OhhO Pilot (VR + mobile, unlimited)",
      "OhhO Fleet (up to 100 robots, OTA)",
      "OhhO Data (unlimited + annotation)",
      "OhhO Serve (10K API calls/day)",
      "Priority support + Slack channel",
    ],
    cta: "Deploy Your Fleet",
    ctaStyle: "violet",
    variant: "fleet",
  },
  {
    name: "Forge",
    hl: "Enterprise / white-label",
    price: "Custom",
    period: "Contact for pricing",
    features: [
      "Everything in Fleet",
      "Unlimited robots",
      "On-prem OhhO Serve license",
      "Custom robot profile integration",
      "Dedicated SLA + onboarding",
      "White-label OhhO Pilot",
    ],
    cta: "Talk to Us",
    ctaStyle: "outline",
  },
];

export default function Pricing() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

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
          <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4">
            Simple pricing.<br />Scale as you grow.
          </h2>
          <p className="text-[16px] leading-[1.7] max-w-[520px] mx-auto" style={{ color: "rgba(255,255,255,0.52)" }}>
            All plans include OhhO Frame. Add products à la carte.
          </p>
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
  const { ref, inView } = useScrollReveal();
  const isPop = plan.variant === "pop";
  const isFleet = plan.variant === "fleet";
  const isViolet = isFleet;

  const cardStyle: React.CSSProperties = {
    background: isPop
      ? "linear-gradient(175deg, rgba(0,212,255,.06) 0%, var(--surf) 38%)"
      : isFleet
      ? "linear-gradient(175deg, rgba(124,58,237,.07) 0%, var(--surf) 38%)"
      : "var(--surf)",
    border: isPop
      ? "1px solid rgba(0,212,255,.32)"
      : isFleet
      ? "1px solid rgba(124,58,237,.32)"
      : "1px solid rgba(255,255,255,0.07)",
    opacity: inView ? 1 : 0,
    transform: inView ? "translateY(0)" : "translateY(22px)",
    transition: "opacity 0.65s ease, transform 0.65s ease, box-shadow 0.3s, border-color 0.3s",
    transitionDelay: `${delay}s`,
  };

  return (
    <div
      ref={ref}
      className="relative flex flex-col gap-[22px] p-[26px_22px] rounded-[14px]"
      style={cardStyle}
      onMouseEnter={(e) => {
        const el = e.currentTarget as HTMLElement;
        el.style.transform = "translateY(-3px)";
        if (isPop) el.style.boxShadow = "0 0 48px rgba(0,212,255,0.22), 0 20px 56px rgba(0,0,0,.5)";
        else if (isFleet) el.style.boxShadow = "0 0 48px rgba(124,58,237,0.22), 0 20px 56px rgba(0,0,0,.5)";
        else el.style.boxShadow = "0 8px 32px rgba(0,0,0,.5)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget as HTMLElement;
        el.style.transform = inView ? "translateY(0)" : "translateY(22px)";
        el.style.boxShadow = "";
      }}
    >
      {plan.badge && (
        <div
          className="absolute -top-[11px] left-1/2 -translate-x-1/2 font-mono text-[9px] font-semibold px-3 py-[3px] rounded-full whitespace-nowrap tracking-[0.07em]"
          style={{ background: "var(--cyan)", color: "var(--bg)" }}
        >
          {plan.badge}
        </div>
      )}

      <div>
        <div className="font-display text-[19px] font-bold">{plan.name}</div>
        <div className="text-[11px] mt-[3px]" style={{ color: "rgba(255,255,255,0.52)" }}>{plan.hl}</div>
      </div>

      <div>
        <div
          className="font-display font-bold tracking-[-0.03em] leading-none"
          style={{ fontSize: plan.price === "Custom" ? "30px" : "44px", marginTop: plan.price === "Custom" ? "6px" : 0 }}
        >
          {plan.price !== "Free" && plan.price !== "Custom" && (
            <sup className="text-[18px] font-medium align-super leading-[2.2]" style={{ color: "rgba(255,255,255,0.52)" }}>$</sup>
          )}
          {plan.price === "Free" || plan.price === "Custom" ? plan.price : plan.price.replace("$", "")}
        </div>
        <div className="text-[12px] mt-[5px]" style={{ color: "rgba(255,255,255,0.52)" }}>{plan.period}</div>
      </div>

      <div className="flex flex-col gap-[9px] flex-1">
        {plan.features.map((f) => (
          <div key={f} className="flex items-start gap-[10px] text-[12px] leading-[1.55]" style={{ color: "rgba(255,255,255,0.52)" }}>
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

      <CtaButton style={plan.ctaStyle as string} label={plan.cta} />
    </div>
  );
}

function CtaButton({ style, label }: { style: string; label: string }) {
  const base = "block text-center py-[11px] rounded-lg text-[13px] font-semibold cursor-pointer transition-all duration-200";

  if (style === "cyan") {
    return (
      <a
        href="#"
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
        href="#"
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
      href="#"
      className={base}
      style={{ border: "1px solid rgba(255,255,255,0.13)", color: "var(--text)" }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.28)";
        (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,.04)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.13)";
        (e.currentTarget as HTMLElement).style.background = "";
      }}
    >
      {label}
    </a>
  );
}
