"use client";

/*
 * Console hub — landing page for subscribed users.
 *
 * Now redesigned with the multi-robot garage:
 *   1. Shows the user's garage (all robots in their fleet) at the top.
 *   2. Below it, a "tools" section with all OhhO product consoles.
 *   3. Clicking a robot in the garage filters the tools relevant to it.
 *
 * Gated by ConsoleGate (sign-in required; no paid plan needed).
 */

import { Suspense, useCallback, useEffect, useState } from "react";
import { Loader2, Bot, Plug } from "lucide-react";
import { useSearchParams } from "next/navigation";

import Nav from "@/components/Nav";
import ConsoleGate from "@/components/auth/ConsoleGate";
import GlassCard from "@/components/GlassCard";
import GarageView from "@/components/garage/GarageView";
import RobotSelector from "@/components/garage/RobotSelector";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";
import { PRODUCTS } from "@/lib/products";
import { getUserRobots, addUserRobot, deleteUserRobot } from "@/lib/garage/client";
import type { UserRobot } from "@/lib/garage/types";

const consoles = PRODUCTS.filter((p) => p.app);

export default function ConsolePage() {
  return (
    <ConsoleGate product="console">
      <Suspense fallback={<ConsoleShell />}>
        <ConsoleHub />
      </Suspense>
    </ConsoleGate>
  );
}

function ConsoleHub() {
  const { user, subscription } = useAuth();
  const active = hasConsoleAccess(subscription);
  const planName = active ? subscription?.plan ?? "" : "";
  const searchParams = useSearchParams();
  const selectedRobotId = searchParams.get("robot") ?? null;

  const [robots, setRobots] = useState<UserRobot[]>([]);
  const [loadingRobots, setLoadingRobots] = useState(true);
  const [showSelector, setShowSelector] = useState(false);

  const loadRobots = useCallback(async () => {
    setLoadingRobots(true);
    const data = await getUserRobots();
    setRobots(data);
    setLoadingRobots(false);
  }, []);

  useEffect(() => {
    loadRobots();
  }, [loadRobots]);

  const handleAdd = async (name: string, robotTypeId: string, hardwareModelId: string) => {
    const newRobot = await addUserRobot(name, robotTypeId, hardwareModelId);
    if (newRobot) {
      setRobots((prev) => [newRobot, ...prev]);
    }
    setShowSelector(false);
  };

  const handleDelete = async (id: string) => {
    const ok = await deleteUserRobot(id);
    if (ok) {
      setRobots((prev) => prev.filter((r) => r.id !== id));
    }
  };

  const categories = Array.from(new Set(consoles.map((p) => p.category)));

  const consoleHref = (href: string) =>
    selectedRobotId ? `${href}?robot=${encodeURIComponent(selectedRobotId)}` : href;

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="relative z-10 max-w-6xl mx-auto px-6">
          {/* header */}
          <div className="mb-8">
            <h1 className="font-display font-bold text-[clamp(28px,4vw,42px)] tracking-tight mb-2">
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

          {/* ── GARAGE SECTION ── */}
          <div className="mb-12">
            {loadingRobots ? (
              <div className="flex items-center gap-2 py-8" style={{ color: "rgba(255,255,255,0.4)" }}>
                <Loader2 size={16} className="animate-spin" />
                <span className="text-[13px] font-mono">Loading garage…</span>
              </div>
            ) : (
              <GarageView
                robots={robots}
                onAddRobot={() => setShowSelector(true)}
                onDeleteRobot={handleDelete}
                selectedRobotId={selectedRobotId}
              />
            )}
          </div>

          {/* ── TOOLS SECTION ── */}
          {selectedRobotId && (
            <div
              className="mb-6 px-4 py-3 rounded-xl flex items-center gap-3"
              style={{ background: "rgba(0,212,255,0.06)", border: "1px solid rgba(0,212,255,0.15)" }}
            >
              <Bot size={16} style={{ color: "var(--cyan)" }} />
              <div>
                <span className="text-[13px] font-semibold">
                  {robots.find((r) => r.id === selectedRobotId)?.name ?? "Robot"} selected
                </span>
                <span className="text-[11px] ml-2" style={{ color: "rgba(255,255,255,0.4)" }}>
                  — consoles below are pre-configured for this robot
                </span>
              </div>
            </div>
          )}

          <div>
            <h2 className="text-[11px] font-mono uppercase tracking-[0.1em] mb-4" style={{ color: "var(--faint)" }}>
              OhhO Tools & Consoles
            </h2>
            <p className="text-[12px] mb-6" style={{ color: "rgba(255,255,255,0.35)" }}>
              {selectedRobotId
                ? "Select a robot in your garage above to filter tools by compatibility, or launch any console below."
                : "Launch any OhhO product console below. Select a robot from your garage first for a pre-configured experience."}
            </p>
          </div>

          {/* OhhO Link — AI agent connector */}
          <div className="mb-8">
            <a href="/link">
              <GlassCard accent="cyan" interactive padding="20px" radius={16}>
                <div className="flex items-start gap-3">
                  <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ color: "var(--cyan)", background: "rgba(0,212,255,0.12)", border: "1px solid rgba(0,212,255,0.24)" }}>
                    <Plug size={18} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-[14px]">OhhO Link</span>
                      <span className="inline-flex items-center gap-1 text-[8.5px] font-mono font-semibold px-1.5 py-[2px] rounded-full" style={{ background: "rgba(0,212,255,0.16)", color: "var(--cyan)", border: "1px solid rgba(0,212,255,0.32)" }}>
                        <span className="badge-dot" style={{ background: "var(--cyan)", width: 5, height: 5 }} /> NEW
                      </span>
                    </div>
                    <div className="text-[12px] mt-0.5 leading-[1.5]" style={{ color: "rgba(255,255,255,0.52)" }}>
                      Connect any AI agent (Claude, OpenCode, Cursor, Cline) to your robots — 91 MCP tools
                    </div>
                  </div>
                </div>
              </GlassCard>
            </a>
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
                    <a key={p.slug} href={consoleHref(p.app?.href ?? `/${p.slug}`)}>
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

      {/* robot selector modal */}
      {showSelector && (
        <RobotSelector
          onAdd={handleAdd}
          onClose={() => setShowSelector(false)}
        />
      )}
    </>
  );
}

function capitalize(s: string): string {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}

/** Skeleton shell shown while useSearchParams suspense resolves. */
function ConsoleShell() {
  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />
        <div className="relative z-10 max-w-6xl mx-auto px-6">
          <div className="mb-8">
            <div className="font-display font-bold text-[clamp(28px,4vw,42px)] tracking-tight mb-2">
              OhhO Console
            </div>
          </div>
          <div className="flex items-center gap-2 py-12" style={{ color: "rgba(255,255,255,0.4)" }}>
            <Loader2 size={18} className="animate-spin" />
            <span className="text-[14px] font-mono">Loading…</span>
          </div>
        </div>
      </main>
    </>
  );
}
