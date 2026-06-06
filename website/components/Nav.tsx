"use client";

import { useEffect, useState } from "react";

export default function Nav() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const handler = () => setScrolled(window.scrollY > 50);
    window.addEventListener("scroll", handler, { passive: true });
    return () => window.removeEventListener("scroll", handler);
  }, []);

  return (
    <nav
      className="fixed top-0 left-0 right-0 z-[100] flex items-center justify-between transition-all duration-300"
      style={{
        padding: "0 40px",
        height: "62px",
        background: scrolled ? "rgba(10,14,26,.88)" : "transparent",
        backdropFilter: scrolled ? "blur(20px)" : "none",
        WebkitBackdropFilter: scrolled ? "blur(20px)" : "none",
        borderBottom: scrolled ? "1px solid rgba(255,255,255,0.07)" : "1px solid transparent",
      }}
    >
      <a
        href="#home"
        className="font-display font-bold text-[21px] tracking-tight flex items-center group"
      >
        <span className="text-cyan transition-all duration-350 group-hover:scale-110 group-hover:[text-shadow:0_0_16px_rgba(0,212,255,0.22)] inline-block">O</span>
        <span>hh</span>
        <span className="text-cyan transition-all duration-350 group-hover:scale-110 group-hover:[text-shadow:0_0_16px_rgba(0,212,255,0.22)] inline-block">O</span>
      </a>

      <div className="hidden md:flex items-center gap-1">
        {["Products", "Pricing", "How it Works", "Docs", "GitHub"].map((link) => (
          <a
            key={link}
            href={link === "Products" ? "#products" : link === "Pricing" ? "#pricing" : link === "How it Works" ? "#how" : "#"}
            className="text-sm font-medium px-[13px] py-[7px] rounded-md transition-all duration-200 hover:bg-white/5"
            style={{ color: "rgba(255,255,255,0.52)" }}
            onMouseEnter={(e) => (e.currentTarget.style.color = "#fff")}
            onMouseLeave={(e) => (e.currentTarget.style.color = "rgba(255,255,255,0.52)")}
          >
            {link}
          </a>
        ))}
        <a
          href="#"
          className="text-[13px] font-semibold ml-[10px] px-5 py-2 rounded-lg transition-all duration-200 hover:opacity-90 hover:-translate-y-px"
          style={{
            background: "var(--cyan)",
            color: "var(--bg)",
            boxShadow: "0 0 0 rgba(0,212,255,0)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLElement).style.boxShadow = "0 6px 20px rgba(0,212,255,0.22)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLElement).style.boxShadow = "0 0 0 rgba(0,212,255,0)";
          }}
        >
          Get Started
        </a>
      </div>
    </nav>
  );
}
