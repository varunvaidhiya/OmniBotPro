"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";

const steps = [
  {
    n: "1",
    title: "Bring any hardware",
    desc: "Start from a pre-loaded profile, design one in OhhO Build, or connect a robot you already own. Any ROS 2-compatible hardware works.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
        <circle cx="12" cy="16" r="1" fill="var(--cyan)" stroke="none"/>
      </svg>
    ),
  },
  {
    n: "2",
    title: "Make it intelligent",
    desc: "Add perception, training and embodied AI in one command — simulation, CI/CD, VLA inference and observability. Docker + ROS 2, pre-wired.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="4 17 10 11 4 5"/><line x1="12" x2="20" y1="19" y2="19"/>
      </svg>
    ),
  },
  {
    n: "3",
    title: "Operate from anywhere",
    desc: "Use OhhO Pilot on mobile or VR. Monitor your fleet with full observability from any browser, anywhere in the world.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="10"/><line x1="2" x2="22" y1="12" y2="12"/>
        <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
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
              Workflow
            </div>
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] legible">
              Up and running in hours,<br />not months.
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
