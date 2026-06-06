"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";

const stats = [
  { n: "25K+", label: "Lines of production code" },
  { n: "6", label: "Standalone products" },
  { n: "9-DOF", label: "Unified robot policy" },
  { n: "Jazzy", label: "Built on ROS 2 — production ready" },
];

export default function StatsBar() {
  return (
    <div style={{ borderTop: "1px solid rgba(255,255,255,0.07)", borderBottom: "1px solid rgba(255,255,255,0.07)" }}>
      <div className="grid grid-cols-2 md:grid-cols-4">
        {stats.map((s, i) => (
          <StatItem key={s.n} stat={s} delay={i * 0.08} last={i === stats.length - 1} />
        ))}
      </div>
    </div>
  );
}

function StatItem({ stat, delay, last }: { stat: { n: string; label: string }; delay: number; last: boolean }) {
  const { ref, inView } = useScrollReveal();

  return (
    <div
      ref={ref}
      className="text-center transition-all duration-[650ms]"
      style={{
        padding: "44px 28px",
        borderRight: last ? "none" : "1px solid rgba(255,255,255,0.07)",
        opacity: inView ? 1 : 0,
        transform: inView ? "translateY(0)" : "translateY(22px)",
        transitionDelay: `${delay}s`,
      }}
    >
      <div
        className="font-display font-bold text-[34px] tracking-tight mb-[6px]"
        style={{ color: "var(--cyan)" }}
      >
        {stat.n}
      </div>
      <div className="text-[13px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.52)" }}>
        {stat.label}
      </div>
    </div>
  );
}
