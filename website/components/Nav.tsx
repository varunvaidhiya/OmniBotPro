"use client";

import { Sparkles } from "lucide-react";
import { useEffect, useState } from "react";
import { DOCS_HREF, GITHUB_HREF, PRODUCTS_HREF, PRICING_HREF, UPGRADE_HREF, OS_HREF } from "@/lib/site";
import ConsoleNavButton from "@/components/auth/ConsoleNavButton";
import UserMenu from "@/components/auth/UserMenu";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

// Marketing links shown to visitors (not signed in). "OhhO OS" leads — it's the
// open engine that sits on top of (and powers) every product.
const MARKETING_LINKS = ["OhhO OS", "Products", "Pricing", "How it Works", "Docs", "GitHub", "About", "Team", "News"];
// Lean links for any signed-in user — no product/pricing/team marketing clutter.
// OhhO OS + Docs stay available after sign-in (the engine + public reference).
const DEV_LINKS = ["OhhO OS", "Docs", "Link"];

export default function Nav() {
  const [scrolled, setScrolled] = useState(false);
  const { user, subscription } = useAuth();
  const signedIn = Boolean(user);
  const subscribed = Boolean(user && hasConsoleAccess(subscription));

  useEffect(() => {
    const handler = () => setScrolled(window.scrollY > 50);
    window.addEventListener("scroll", handler, { passive: true });
    return () => window.removeEventListener("scroll", handler);
  }, []);

  const navHref = (link: string): string => {
    switch (link) {
      case "OhhO OS": return OS_HREF;
      case "Products": return PRODUCTS_HREF;
      case "Pricing": return PRICING_HREF;
      case "How it Works": return "/#how";
      case "Link": return "/link";
      case "Docs": return DOCS_HREF;
      case "GitHub": return GITHUB_HREF;
      case "About": return "/about";
      case "Team": return "/team";
      case "News": return "/news/ohho-mind";
      default: return "#";
    }
  };

  const links = signedIn ? DEV_LINKS : MARKETING_LINKS;

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
        href="/"
        className="font-display font-bold text-[21px] tracking-tight flex items-center group"
      >
        <span className="text-cyan transition-all duration-350 group-hover:scale-110 group-hover:[text-shadow:0_0_16px_rgba(0,212,255,0.22)] inline-block">O</span>
        <span>hh</span>
        <span className="text-cyan transition-all duration-350 group-hover:scale-110 group-hover:[text-shadow:0_0_16px_rgba(0,212,255,0.22)] inline-block">O</span>
      </a>

      <div className="hidden md:flex items-center gap-1">
        {links.map((link) => {
          const isOS = link === "OhhO OS";
          const base = isOS ? "var(--cyan)" : "rgba(255,255,255,0.52)";
          return (
            <a
              key={link}
              href={navHref(link)}
              target={link === "GitHub" ? "_blank" : undefined}
              rel={link === "GitHub" ? "noopener noreferrer" : undefined}
              className="text-sm font-medium px-[13px] py-[7px] rounded-md transition-all duration-200 hover:bg-white/5 whitespace-nowrap"
              style={{ color: base }}
              onMouseEnter={(e) => (e.currentTarget.style.color = "#fff")}
              onMouseLeave={(e) => (e.currentTarget.style.color = base)}
            >
              {link}
            </a>
          );
        })}
        {user && (
          <a
            href="/garage"
            className="text-sm font-medium px-[13px] py-[7px] rounded-md transition-all duration-200 hover:bg-white/5"
            style={{ color: "var(--cyan)", opacity: 0.85 }}
            onMouseEnter={(e) => (e.currentTarget.style.opacity = "1")}
            onMouseLeave={(e) => (e.currentTarget.style.opacity = "0.85")}
          >
            Garage
          </a>
        )}
        {/* Visitor → Get Started; signed-in free user → highlighted Upgrade */}
        {!signedIn && (
          <a
            href="/#pricing"
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
        )}
        {signedIn && !subscribed && (
          <a
            href={UPGRADE_HREF}
            className="inline-flex items-center gap-1.5 text-[13px] font-semibold ml-[10px] px-5 py-2 rounded-lg transition-all duration-200 hover:opacity-90 hover:-translate-y-px"
            style={{
              background: "var(--cyan)",
              color: "var(--bg)",
              boxShadow: "0 4px 18px rgba(0,212,255,0.30)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "0 8px 26px rgba(0,212,255,0.42)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "0 4px 18px rgba(0,212,255,0.30)";
            }}
          >
            <Sparkles size={14} strokeWidth={2.5} />
            Upgrade
          </a>
        )}
        {/* console hub: adapts to auth + subscription state */}
        <span className="ml-[10px]"><ConsoleNavButton /></span>
        {/* auth control: renders nothing until Supabase is configured */}
        <span className="ml-[10px]"><UserMenu /></span>
      </div>
    </nav>
  );
}
