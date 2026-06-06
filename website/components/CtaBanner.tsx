"use client";

import { useScrollReveal } from "@/hooks/useScrollReveal";
import { ArrowRight } from "lucide-react";

export default function CtaBanner() {
  const { ref, inView } = useScrollReveal();

  return (
    <section className="relative overflow-hidden" style={{ padding: "112px 24px" }}>
      <div className="cta-bg" />
      <div className="max-w-content mx-auto">
        <div
          ref={ref}
          className="text-center max-w-[600px] mx-auto transition-all duration-[650ms]"
          style={{ opacity: inView ? 1 : 0, transform: inView ? "none" : "translateY(22px)" }}
        >
          <h2
            className="font-display font-bold tracking-[-0.035em] leading-[1.08] mb-[18px]"
            style={{ fontSize: "clamp(40px, 7vw, 72px)" }}
          >
            Ready to<br />operate?
          </h2>
          <p className="text-[16px] mb-10 leading-[1.65]" style={{ color: "rgba(255,255,255,0.52)" }}>
            Start free. No hardware required — simulate first.
          </p>
          <a
            href="#"
            className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg mx-auto transition-all duration-200 hover:-translate-y-0.5 hover:opacity-95"
            style={{ background: "var(--cyan)", color: "var(--bg)" }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "0 10px 32px rgba(0,212,255,0.22)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "";
            }}
          >
            Get Started with OhhO <ArrowRight size={15} strokeWidth={2.5} />
          </a>
        </div>
      </div>
    </section>
  );
}
