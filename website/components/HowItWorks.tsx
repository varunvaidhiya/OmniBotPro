"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";

const steps = [
  {
    n: "1",
    title: "Open source — yours to keep",
    desc: "Clone the repo, self-host every console, or sign in to use OhhO Cloud. MIT/Apache licensed — no lock-in. Works with any ROS 2 hardware out of the box.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="10"/><path d="M8.56 2.75c4.37 6.03 6.02 9.42 8.03 17.72m2.54-15.38c-3.72 4.35-8.94 5.66-16.88 5.85m19.5 1.9c-3.5-.93-6.63-.82-8.94 0-2.58.92-5.01 2.86-7.44 6.32"/>
      </svg>
    ),
  },
  {
    n: "2",
    title: "Build and train — free locally",
    desc: "Design robots in OhhO Build, collect episodes with OhhO Data, fine-tune VLA or RL policies locally. Need cloud GPUs? Pay only for the hours you run.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="4 17 10 11 4 5"/><line x1="12" x2="20" y1="19" y2="19"/>
      </svg>
    ),
  },
  {
    n: "3",
    title: "Pay only for what you run",
    desc: "Cloud GPU training, AI inference API, MCP server hosting, cloud simulation — each billed per use. Everything else: free. No surprise bills, no minimum spend.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="5" width="20" height="14" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/>
      </svg>
    ),
  },
];

export default function HowItWorks() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="how" style={{ padding: "112px 24px" }}>
      <div className="max-w-content mx-auto">
        <div className="text-center mb-[60px]">
          <div
            ref={headRef}
            className="transition-all duration-[650ms]"
            style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
          >
            <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--cyan)" }}>
              How it works
            </div>
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] legible">
              Open platform.<br />Pay-as-you-go cloud.
            </h2>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {steps.map((step, i) => (
            <StepItem key={step.n} step={step} delay={[0, 0.16, 0.32][i]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function StepItem({ step, delay }: { step: typeof steps[0]; delay: number }) {
  return (
    <GlassCard
      accent="cyan"
      delay={delay}
      padding="40px 28px"
      className="flex flex-col items-center text-center"
    >
      <div
        className="glass-pop relative w-20 h-20 rounded-full flex items-center justify-center mb-7 flex-shrink-0"
        style={{
          background: "rgba(0,212,255,.08)",
          border: "1px solid rgba(0,212,255,.24)",
          boxShadow: "inset 0 0 30px rgba(0,212,255,.16)",
        }}
      >
        <span
          className="absolute -top-1 -right-1 w-5 h-5 rounded-full flex items-center justify-center font-mono text-[10px] font-medium"
          style={{ background: "var(--cyan)", color: "var(--bg)", boxShadow: "0 2px 10px rgba(0,212,255,.45)" }}
        >
          {step.n}
        </span>
        {step.icon}
      </div>
      <div className="font-display text-[18px] font-semibold mb-3">{step.title}</div>
      <p className="text-[14px] leading-[1.7]" style={{ color: "rgba(255,255,255,0.66)" }}>
        {step.desc}
      </p>
    </GlassCard>
  );
}
