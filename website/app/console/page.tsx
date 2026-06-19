"use client";

/*
 * Console hub — landing page for subscribed users. Lists every product console
 * so users can jump straight into any tool without going through the marketing site.
 *
 * Gated by ConsoleGate (auth + subscription required).
 */

import Nav from "@/components/Nav";
import ConsoleGate from "@/components/auth/ConsoleGate";
import GlassCard from "@/components/GlassCard";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";
import { PRODUCTS } from "@/lib/products";

const consoles = PRODUCTS.filter((p) => p.app);

export default function ConsolePage() {
  return (
    <ConsoleGate product="console">
      <ConsoleHub />
    </ConsoleGate>
  );
}

function ConsoleHub() {
  const { user, subscription } = useAuth();
  const active = hasConsoleAccess(subscription);
  const planName = active ? subscription?.plan ?? "" : "";

  const categories = Array.from(new Set(consoles.map((p) => p.category)));

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="relative z-10 max-w-6xl mx-auto px-6">
          <div className="mb-10">
            <h1 className="font-display font-bold text-[clamp(28px,4vw,42px)] tracking-tight mb-2 legible">
              OhhO Console
            </h1>
            <p className="text-[14px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.6)" }}>
              {user?.email}
              {active && planName && (
                <span className="ml-2 inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-mono" style={{ background: "var(--cyan)", color: "var(--bg)" }}>
                  {capitalize(planName)} plan
                </span>
              )}
            </p>
          </div>

          {categories.map((category) => (
            <div key={category} className="mb-10">
              <h2 className="text-[11px] font-mono uppercase tracking-[0.1em] mb-4" style={{ color: "var(--faint)" }}>
                {category}
              </h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {consoles
                  .filter((p) => p.category === category)
                  .map((p) => (
                    <a key={p.slug} href={p.app?.href ?? `/${p.slug}`}>
                      <GlassCard accent={p.accent} interactive padding="20px" radius={16}>
                        <div className="flex items-start gap-3">
                          <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ color: p.accent === "cyan" ? "var(--cyan)" : "var(--violet-lite)" }}>
                            {p.icon}
                          </div>
                          <div className="min-w-0">
                            <div className="font-semibold text-[14px] truncate">{p.name}</div>
                            <div className="text-[12px] mt-0.5 leading-[1.5]" style={{ color: "rgba(255,255,255,0.52)" }}>
                              {p.tag}
                            </div>
                          </div>
                        </div>
                      </GlassCard>
                    </a>
                  ))}
              </div>
            </div>
          ))}
        </div>
      </main>
    </>
  );
}

function capitalize(s: string): string {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}
