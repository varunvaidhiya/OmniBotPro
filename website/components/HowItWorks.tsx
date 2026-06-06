"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";

const steps = [
  {
    n: "1",
    title: "Pick your robot",
    desc: "Choose from pre-loaded profiles or build your own with OhhO Frame. Works with any ROS 2 compatible hardware.",
    icon: (
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--cyan)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
        <circle cx="12" cy="16" r="1" fill="var(--cyan)" stroke="none"/>
      </svg>
    ),
  },
  {
    n: "2",
    title: "Deploy your stack",
    desc: "One command launches your full stack — simulation, CI/CD, AI inference, and observability. Docker + ROS 2, pre-wired.",
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
    <section
      id="how"
      style={{ padding: "112px 24px", background: "var(--surf)" }}
    >
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
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12]">
              Up and running in hours,<br />not months.
            </h2>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-10 md:gap-0 relative">
          <div className="step-join hidden md:block" />
          {steps.map((step, i) => (
            <StepItem key={step.n} step={step} delay={[0, 0.16, 0.32][i]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function StepItem({ step, delay }: { step: typeof steps[0]; delay: number }) {
  const { ref, inView } = useScrollReveal();

  return (
    <div
      ref={ref}
      className="flex flex-col items-center text-center px-8 transition-all duration-[650ms]"
      style={{
        opacity: inView ? 1 : 0,
        transform: inView ? "none" : "translateY(22px)",
        transitionDelay: `${delay}s`,
      }}
    >
      <div
        className="relative w-20 h-20 rounded-full flex items-center justify-center mb-7 flex-shrink-0 z-[2]"
        style={{ background: "var(--bg)", border: "1px solid rgba(255,255,255,0.13)" }}
      >
        <span
          className="absolute -top-1 -right-1 w-5 h-5 rounded-full flex items-center justify-center font-mono text-[10px] font-medium"
          style={{ background: "var(--cyan)", color: "var(--bg)" }}
        >
          {step.n}
        </span>
        {step.icon}
      </div>
      <div className="font-display text-[18px] font-semibold mb-3">{step.title}</div>
      <p className="text-[14px] leading-[1.7]" style={{ color: "rgba(255,255,255,0.52)" }}>
        {step.desc}
      </p>
    </div>
  );
}
