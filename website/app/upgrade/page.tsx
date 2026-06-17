"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ArrowLeft, Check, Loader2, Lock } from "lucide-react";

import Nav from "@/components/Nav";
import GlassCard from "@/components/GlassCard";
import { useAuth } from "@/lib/auth/AuthProvider";
import { getSupabase } from "@/lib/auth/supabase";
import { PLANS, getPlan, hasConsoleAccess, type Plan } from "@/lib/auth/plans";
import { contactMailto } from "@/lib/site";

export default function UpgradePage() {
  const { configured, loading, user, subscription } = useAuth();
  const router = useRouter();
  const [next, setNext] = useState("/account");
  const [product, setProduct] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setNext(params.get("next") || "/account");
    setProduct(params.get("product"));
  }, []);

  // must be signed in to subscribe
  useEffect(() => {
    if (configured && !loading && !user) {
      router.replace(`/login?next=${encodeURIComponent("/upgrade")}`);
    }
  }, [configured, loading, user, router]);

  // already subscribed → go straight to the destination
  useEffect(() => {
    if (configured && hasConsoleAccess(subscription)) router.replace(next);
  }, [configured, subscription, next, router]);

  const subscribe = async (plan: Plan) => {
    if (!plan.checkout) {
      window.location.href = contactMailto(`OhhO ${plan.name} plan`);
      return;
    }
    const supabase = getSupabase();
    if (!supabase) return;
    setBusy(plan.id);
    setError(null);
    try {
      const { data, error } = await supabase.functions.invoke("create-checkout-session", {
        body: { plan: plan.id, next, origin: window.location.origin },
      });
      if (error) throw error;
      if (data?.url) window.location.href = data.url as string;
      else throw new Error("No checkout URL returned.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not start checkout.");
      setBusy(null);
    }
  };

  const productName = product ? getPlan(product)?.name ?? labelize(product) : null;

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-50" />
        <div className="hero-orb-2 opacity-50" />

        <div className="relative z-10 max-w-5xl mx-auto px-6">
          <Link href={next} className="inline-flex items-center gap-2 text-[12px] font-mono tracking-widest uppercase transition-colors hover:text-white" style={{ color: "var(--cyan)" }}>
            <ArrowLeft size={14} /> Back
          </Link>

          <div className="mt-8 mb-10 max-w-2xl">
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full mb-5" style={{ background: "rgba(0,212,255,.1)", border: "1px solid rgba(0,212,255,.28)" }}>
              <Lock size={13} style={{ color: "var(--cyan)" }} />
              <span className="text-[11.5px] font-mono" style={{ color: "var(--cyan)" }}>
                {product ? `Opening OhhO ${labelize(product)} needs a plan` : "Choose a plan to unlock the consoles"}
              </span>
            </div>
            <h1 className="font-display font-bold text-[clamp(30px,4.5vw,46px)] tracking-tight leading-[1.08] mb-3 legible">
              Pick a plan to launch{productName ? "" : " the consoles"}
            </h1>
            <p className="text-[15px] leading-[1.7]" style={{ color: "rgba(255,255,255,0.66)" }}>
              Every product console — Build, Serve and the rest — unlocks with any paid plan. Cancel anytime.
            </p>
          </div>

          {!configured && (
            <div className="mb-8 p-4 rounded-xl text-[13px] max-w-2xl" style={{ background: "rgba(251,191,36,.08)", border: "1px solid rgba(251,191,36,.4)", color: "rgba(255,255,255,.8)" }}>
              Billing isn&apos;t configured on this deployment yet — add Supabase + Stripe keys (see <span className="font-mono">AUTH_SETUP.md</span>) to enable checkout.
            </div>
          )}
          {error && <div className="mb-6 p-3 rounded-lg text-[13px] max-w-2xl" style={{ background: "rgba(248,113,113,.1)", border: "1px solid rgba(248,113,113,.4)", color: "#fff" }}>{error}</div>}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-[18px] items-start">
            {PLANS.map((plan) => (
              <GlassCard key={plan.id} accent={plan.accent} featured={plan.popular} interactive={false} padding="26px 22px" radius={18} className="relative flex flex-col gap-5">
                {plan.popular && (
                  <div className="absolute -top-[11px] left-1/2 -translate-x-1/2 font-mono text-[9px] font-semibold px-3 py-[3px] rounded-full tracking-[0.07em]" style={{ background: "var(--cyan)", color: "var(--bg)" }}>
                    MOST POPULAR
                  </div>
                )}
                <div>
                  <div className="font-display text-[19px] font-bold">{plan.name}</div>
                  <div className="text-[11.5px] mt-1" style={{ color: "rgba(255,255,255,0.6)" }}>{plan.blurb}</div>
                  <div className="flex items-baseline gap-1.5 mt-4">
                    <span className="font-display text-[34px] font-bold tracking-tight">{plan.price}</span>
                    <span className="text-[12px]" style={{ color: "rgba(255,255,255,0.55)" }}>{plan.period}</span>
                  </div>
                </div>
                <div className="flex flex-col gap-2 flex-1">
                  {plan.features.map((f) => (
                    <div key={f} className="flex items-start gap-2.5 text-[12.5px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.7)" }}>
                      <Check size={13} strokeWidth={2.5} style={{ color: plan.accent === "violet" ? "var(--violet-lite)" : "var(--cyan)", flexShrink: 0, marginTop: 2 }} />
                      {f}
                    </div>
                  ))}
                </div>
                <button
                  onClick={() => subscribe(plan)}
                  disabled={busy !== null || (!configured && plan.checkout)}
                  className="inline-flex items-center justify-center gap-2 py-2.5 rounded-lg text-[13px] font-semibold transition-all disabled:opacity-50 enabled:hover:-translate-y-px"
                  style={
                    plan.accent === "violet"
                      ? { background: "var(--violet)", color: "#fff" }
                      : plan.accent === "cyan"
                        ? { background: "var(--cyan)", color: "var(--bg)" }
                        : { border: "1px solid rgba(255,255,255,0.2)", color: "var(--text)" }
                  }
                >
                  {busy === plan.id ? <Loader2 size={15} className="animate-spin" /> : plan.checkout ? `Subscribe to ${plan.name}` : "Talk to us"}
                </button>
              </GlassCard>
            ))}
          </div>
        </div>
      </main>
    </>
  );
}

function labelize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
