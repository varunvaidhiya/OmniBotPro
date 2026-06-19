"use client";

/*
 * Subscription-aware pricing UI for the product detail page.
 *
 * The product page (app/products/[slug]/page.tsx) is a server component so it
 * can pre-render metadata, but pricing must disappear for subscribed users.
 * These three client islands wrap the pricing-sensitive CTAs and the plan
 * chooser. A subscribed user sees "open the console" instead of prices; a
 * visitor or free user sees the full plan grid exactly as before.
 */

import Link from "next/link";
import { ArrowRight, Check, Minus, Sparkles } from "lucide-react";

import GlassCard from "@/components/GlassCard";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";
import { accentColor, type PlanName, type Product } from "@/lib/products";
import { contactMailto, PRICING_HREF, SIGNUP_HREF, UPGRADE_HREF } from "@/lib/site";

const PLAN_META: Record<PlanName, { price: string; period: string }> = {
  Spark: { price: "Free", period: "forever" },
  Builder: { price: "$49", period: "/ mo" },
  Fleet: { price: "$199", period: "/ mo" },
  Forge: { price: "Custom", period: "contact us" },
};

function useSubscribed() {
  const { user, subscription } = useAuth();
  return Boolean(user && hasConsoleAccess(subscription));
}

function capitalize(s: string): string {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}

// ── Hero CTA buttons ────────────────────────────────────────────────────────

export function ProductHeroCta({ product }: { product: Product }) {
  const subscribed = useSubscribed();
  const aColor = accentColor(product.accent);

  if (subscribed) {
    // No pricing CTAs for subscribed users — just launch the console.
    return (
      <div className="flex flex-wrap items-center gap-3 mt-8">
        {product.app ? (
          <Link
            href={product.app.href}
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            {product.app.label} <ArrowRight size={15} strokeWidth={2.5} />
          </Link>
        ) : (
          <Link
            href="/console"
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            Open the console <ArrowRight size={15} strokeWidth={2.5} />
          </Link>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-wrap items-center gap-3 mt-8">
      {product.app ? (
        <>
          <Link
            href={product.app.href}
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            {product.app.label} <ArrowRight size={15} strokeWidth={2.5} />
          </Link>
          <a
            href="#plans"
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:bg-white/[0.06]"
            style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
          >
            Choose your plan
          </a>
        </>
      ) : (
        <>
          <a
            href="#plans"
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            Choose your plan <ArrowRight size={15} strokeWidth={2.5} />
          </a>
          <Link
            href={PRICING_HREF}
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[22px] py-[12px] rounded-lg transition-all duration-200 hover:bg-white/[0.06]"
            style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
          >
            Compare all plans
          </Link>
        </>
      )}
    </div>
  );
}

// ── Plan chooser section (#plans) ───────────────────────────────────────────

export function ProductPlanSection({ product }: { product: Product }) {
  const { subscription } = useAuth();
  const subscribed = useSubscribed();
  const aColor = accentColor(product.accent);

  if (subscribed) {
    // Replace the plan grid with a simple "you're subscribed" panel — no prices.
    return (
      <section id="plans" className="mt-[88px] scroll-mt-24">
        <GlassCard accent={product.accent} interactive={false} padding="32px" radius={18} className="max-w-[760px]">
          <div className="flex items-start gap-3">
            <Check size={20} style={{ color: aColor, flexShrink: 0, marginTop: 2 }} />
            <div>
              <div className="font-display text-[18px] font-semibold mb-1">
                {product.name} is included in your subscription
              </div>
              <p className="text-[14px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.66)" }}>
                You&apos;re on the {capitalize(subscription?.plan ?? "")} plan. Manage or upgrade your subscription
                anytime from your account.
              </p>
              <div className="flex flex-wrap gap-3 mt-5">
                {product.app ? (
                  <Link
                    href={product.app.href}
                    className="inline-flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg"
                    style={{ background: aColor, color: "var(--bg)" }}
                  >
                    {product.app.label} <ArrowRight size={14} strokeWidth={2.5} />
                  </Link>
                ) : (
                  <Link
                    href="/console"
                    className="inline-flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg"
                    style={{ background: aColor, color: "var(--bg)" }}
                  >
                    Open the console <ArrowRight size={14} strokeWidth={2.5} />
                  </Link>
                )}
                <Link
                  href="/account"
                  className="inline-flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg"
                  style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
                >
                  Manage subscription
                </Link>
              </div>
            </div>
          </div>
        </GlassCard>
      </section>
    );
  }

  return (
    <section id="plans" className="mt-[88px] scroll-mt-24">
      <div className="mb-8">
        <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-3" style={{ color: aColor }}>
          Pricing
        </div>
        <h2 className="font-display font-bold text-[clamp(24px,3.4vw,36px)] tracking-tight leading-[1.15] legible">
          {`Which plan do I need for ${product.name}?`}
        </h2>
      </div>
      <div
        className="flex items-start gap-3 mb-7 p-4 rounded-xl max-w-[760px]"
        style={{ background: "rgba(255,255,255,.03)", border: `1px solid ${product.accent === "cyan" ? "rgba(0,212,255,.22)" : "rgba(124,58,237,.26)"}` }}
      >
        <Sparkles size={18} style={{ color: aColor, flexShrink: 0, marginTop: 2 }} />
        <p className="text-[14px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.78)" }}>
          <span className="font-semibold" style={{ color: "#fff" }}>
            We recommend the {product.recommendedPlan} plan.
          </span>{" "}
          {product.planRationale}
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-[14px]">
        {product.plans.map((pl) => {
          const meta = PLAN_META[pl.plan];
          const recommended = pl.plan === product.recommendedPlan;
          return (
            <GlassCard
              key={pl.plan}
              accent={recommended ? product.accent : "none"}
              featured={recommended}
              interactive={false}
              padding="20px"
              radius={16}
              className="relative flex flex-col"
            >
              {recommended && (
                <div
                  className="absolute -top-[10px] left-1/2 -translate-x-1/2 font-mono text-[8.5px] font-semibold px-2.5 py-[3px] rounded-full whitespace-nowrap tracking-[0.06em]"
                  style={{ background: aColor, color: "var(--bg)" }}
                >
                  RECOMMENDED
                </div>
              )}
              <div className="font-display text-[16px] font-bold">{pl.plan}</div>
              <div className="flex items-baseline gap-1 mt-1.5 mb-4">
                <span className="font-display text-[22px] font-bold">{meta.price}</span>
                <span className="text-[11px]" style={{ color: "rgba(255,255,255,0.5)" }}>
                  {meta.period}
                </span>
              </div>
              <div className="flex items-start gap-2 text-[12.5px] leading-[1.5] flex-1" style={{ color: pl.included ? "rgba(255,255,255,0.78)" : "rgba(255,255,255,0.4)" }}>
                <span
                  className="w-[15px] h-[15px] rounded-full flex-shrink-0 mt-[1px] flex items-center justify-center"
                  style={{
                    background: pl.included ? (product.accent === "cyan" ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)") : "rgba(255,255,255,.05)",
                    border: pl.included ? `1px solid ${aColor}` : "1px solid rgba(255,255,255,.12)",
                  }}
                >
                  {pl.included ? (
                    <Check size={8} strokeWidth={3} style={{ color: aColor }} />
                  ) : (
                    <Minus size={8} strokeWidth={3} style={{ color: "rgba(255,255,255,0.4)" }} />
                  )}
                </span>
                {pl.level}
              </div>
              <a
                href={pl.plan === "Forge" ? contactMailto(`OhhO Forge plan — ${product.name}`) : pl.plan === "Spark" ? SIGNUP_HREF : UPGRADE_HREF}
                className="mt-4 block text-center py-[9px] rounded-lg text-[12.5px] font-semibold transition-all duration-200"
                style={
                  recommended
                    ? { background: aColor, color: "var(--bg)" }
                    : { border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }
                }
              >
                {pl.plan === "Spark" ? "Start free" : pl.plan === "Forge" ? "Talk to us" : `Choose ${pl.plan}`}
              </a>
            </GlassCard>
          );
        })}
      </div>

      <div className="mt-7">
        <Link
          href={PRICING_HREF}
          className="inline-flex items-center gap-2 text-[13px] font-semibold font-mono"
          style={{ color: aColor }}
        >
          See the full pricing comparison <ArrowRight size={13} strokeWidth={2.5} />
        </Link>
      </div>
    </section>
  );
}

// ── Final CTA buttons ───────────────────────────────────────────────────────

export function ProductFinalCta({ product }: { product: Product }) {
  const subscribed = useSubscribed();
  const aColor = accentColor(product.accent);

  if (subscribed) {
    return (
      <div className="flex flex-wrap items-center justify-center gap-3">
        {product.app ? (
          <Link
            href={product.app.href}
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[24px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            {product.app.label} <ArrowRight size={15} strokeWidth={2.5} />
          </Link>
        ) : (
          <Link
            href="/console"
            className="inline-flex items-center gap-2 text-[13px] font-semibold px-[24px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
            style={{ background: aColor, color: "var(--bg)" }}
          >
            Open the console <ArrowRight size={15} strokeWidth={2.5} />
          </Link>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-wrap items-center justify-center gap-3">
      {product.app ? (
        <Link
          href={product.app.href}
          className="inline-flex items-center gap-2 text-[13px] font-semibold px-[24px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
          style={{ background: aColor, color: "var(--bg)" }}
        >
          {product.app.label} <ArrowRight size={15} strokeWidth={2.5} />
        </Link>
      ) : (
        <a
          href={SIGNUP_HREF}
          className="inline-flex items-center gap-2 text-[13px] font-semibold px-[24px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
          style={{ background: aColor, color: "var(--bg)" }}
        >
          Get started free <ArrowRight size={15} strokeWidth={2.5} />
        </a>
      )}
      <Link
        href={PRICING_HREF}
        className="inline-flex items-center gap-2 text-[13px] font-semibold px-[24px] py-[13px] rounded-lg transition-all duration-200 hover:bg-white/[0.06]"
        style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
      >
        View pricing
      </Link>
    </div>
  );
}
