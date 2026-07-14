"use client";

import GlassCard from "@/components/GlassCard";
import { STATS } from "@/lib/copy";

export default function StatsBar() {
  return (
    <div className="max-w-content mx-auto px-6" style={{ paddingTop: "64px", paddingBottom: "8px" }}>
      <GlassCard interactive={false} padding="0" radius={22} className="overflow-hidden">
        <div className="grid grid-cols-2 md:grid-cols-4">
          {STATS.map((s, i) => (
            <StatItem key={s.n} stat={s} last={i === STATS.length - 1} />
          ))}
        </div>
      </GlassCard>
    </div>
  );
}

function StatItem({ stat, last }: { stat: { n: string; label: string }; last: boolean }) {
  return (
    <div
      className="text-center"
      style={{
        padding: "40px 28px",
        borderRight: last ? "none" : "1px solid rgba(255,255,255,0.10)",
      }}
    >
      <div
        className="font-display font-bold text-[34px] tracking-tight mb-[6px]"
        style={{ color: "var(--cyan)" }}
      >
        {stat.n}
      </div>
      <div className="text-[13px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.66)" }}>
        {stat.label}
      </div>
    </div>
  );
}
