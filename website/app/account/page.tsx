"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ArrowLeft, ArrowRight, Check, CreditCard, Loader2, LogOut } from "lucide-react";

import Nav from "@/components/Nav";
import GlassCard from "@/components/GlassCard";
import { useAuth } from "@/lib/auth/AuthProvider";
import { getSupabase } from "@/lib/auth/supabase";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function AccountPage() {
  const { configured, loading, user, subscription, signOut, refreshSubscription } = useAuth();
  const router = useRouter();
  const [justPaid, setJustPaid] = useState(false);
  const [portalBusy, setPortalBusy] = useState(false);

  useEffect(() => {
    if (configured && !loading && !user) router.replace("/login?next=/account");
  }, [configured, loading, user, router]);

  // returning from Stripe Checkout — the webhook may lag, so poll briefly
  useEffect(() => {
    if (new URLSearchParams(window.location.search).get("checkout") === "success") {
      setJustPaid(true);
      let n = 0;
      const iv = setInterval(() => {
        refreshSubscription();
        if (++n >= 5) clearInterval(iv);
      }, 2000);
      return () => clearInterval(iv);
    }
  }, [refreshSubscription]);

  const manageBilling = async () => {
    const supabase = getSupabase();
    if (!supabase) return;
    setPortalBusy(true);
    try {
      const { data, error } = await supabase.functions.invoke("create-portal-session", {
        body: { origin: window.location.origin },
      });
      if (error) throw await unwrapFunctionError(error);
      if (data?.url) window.location.href = data.url as string;
    } catch (e) {
      alert(e instanceof Error ? e.message : "Could not open billing portal.");
    } finally {
      setPortalBusy(false);
    }
  };

  const active = hasConsoleAccess(subscription);

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-2 opacity-50" />

        <div className="relative z-10 max-w-2xl mx-auto px-6">
          <Link href="/" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-widest uppercase transition-colors hover:text-white" style={{ color: "var(--cyan)" }}>
            <ArrowLeft size={14} /> Home
          </Link>

          <h1 className="font-display font-bold text-[clamp(28px,4vw,40px)] tracking-tight mt-8 mb-8 legible">Account</h1>

          {!configured ? (
            <GlassCard interactive={false} padding="24px" radius={18}>
              <p className="text-[14px]" style={{ color: "rgba(255,255,255,.7)" }}>
                Authentication isn&apos;t configured on this deployment. See <span className="font-mono">AUTH_SETUP.md</span>.
              </p>
            </GlassCard>
          ) : loading || !user ? (
            <div className="flex items-center gap-2 text-[13px]" style={{ color: "var(--faint)" }}>
              <Loader2 size={16} className="animate-spin" /> Loading…
            </div>
          ) : (
            <div className="flex flex-col gap-4">
              {justPaid && (
                <div className="p-4 rounded-xl flex items-center gap-3" style={{ background: "rgba(52,211,153,.08)", border: "1px solid rgba(52,211,153,.4)" }}>
                  <Check size={18} style={{ color: "#34D399" }} />
                  <span className="text-[13.5px]" style={{ color: "rgba(255,255,255,.85)" }}>Payment received — your consoles are unlocking{active ? "" : " (this can take a few seconds)"}.</span>
                </div>
              )}

              <GlassCard interactive={false} padding="24px" radius={18}>
                <div className="text-[11px] font-mono uppercase tracking-wider mb-2" style={{ color: "var(--faint)" }}>Signed in as</div>
                <div className="text-[16px] font-semibold">{user.email}</div>
              </GlassCard>

              <GlassCard accent={active ? "cyan" : "none"} interactive={false} padding="24px" radius={18}>
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-[11px] font-mono uppercase tracking-wider mb-2" style={{ color: "var(--faint)" }}>Plan</div>
                    <div className="text-[18px] font-display font-bold">
                      {active ? capitalize(subscription?.plan ?? "") : "No active plan"}
                    </div>
                    <div className="text-[12.5px] mt-1" style={{ color: active ? "#34D399" : "var(--muted)" }}>
                      {active
                        ? `Status: ${subscription?.status}${subscription?.current_period_end ? ` · renews ${new Date(subscription.current_period_end).toLocaleDateString()}` : ""}`
                        : "Subscribe to unlock the product consoles."}
                    </div>
                  </div>
                </div>
                <div className="flex flex-wrap gap-2.5 mt-5">
                  {active ? (
                    <>
                      <Link href="/build" className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg" style={{ background: "var(--cyan)", color: "var(--bg)" }}>
                        Open OhhO Build <ArrowRight size={14} strokeWidth={2.5} />
                      </Link>
                      <Link href="/serve" className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg" style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}>
                        Open OhhO Serve
                      </Link>
                      <button onClick={manageBilling} disabled={portalBusy} className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg disabled:opacity-50" style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}>
                        {portalBusy ? <Loader2 size={14} className="animate-spin" /> : <CreditCard size={14} />} Manage billing
                      </button>
                    </>
                  ) : (
                    <Link href="/upgrade" className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg" style={{ background: "var(--cyan)", color: "var(--bg)" }}>
                      Choose a plan <ArrowRight size={14} strokeWidth={2.5} />
                    </Link>
                  )}
                </div>
              </GlassCard>

              <button onClick={() => signOut()} className="self-start inline-flex items-center gap-2 text-[13px] font-medium px-4 py-2.5 rounded-lg transition-colors hover:bg-white/[0.06]" style={{ border: "1px solid rgba(255,255,255,0.14)", color: "var(--muted)" }}>
                <LogOut size={14} /> Sign out
              </button>
            </div>
          )}
        </div>
      </main>
    </>
  );
}

function capitalize(s: string): string {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}

async function unwrapFunctionError(error: unknown): Promise<Error> {
  try {
    const ctx = (error as { context?: Response }).context;
    if (ctx) {
      const body = await ctx.json().catch(() => null);
      if (body?.error) return new Error(String(body.error));
      const text = await ctx.text().catch(() => "");
      if (text) return new Error(text);
    }
  } catch {
    /* fall through */
  }
  return error instanceof Error ? error : new Error("Request failed.");
}
