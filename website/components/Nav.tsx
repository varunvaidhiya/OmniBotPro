"use client";

import { Sparkles, ChevronDown, Menu, X } from "lucide-react";
import { useEffect, useState } from "react";
import { DOCS_HREF, GITHUB_HREF, NO_LOCKIN_HREF, PRODUCTS_HREF, PRICING_HREF, STANDARDS_HREF, UPGRADE_HREF, OS_HREF, WHY_HREF, START_HREF, SERVICES_HREF } from "@/lib/site";
import ConsoleNavButton from "@/components/auth/ConsoleNavButton";
import UserMenu from "@/components/auth/UserMenu";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

// Marketing nav for visitors. Kept to a tight primary row — "No Lock-In" leads
// (the brand-agnostic moat), "Why OhhO" is the positioning, "OhhO OS" is the open
// engine — with the secondary/company links tucked under a "More" dropdown so the
// bar stays uncrowded.
const PRIMARY_LINKS = ["No Lock-In", "Why OhhO", "OhhO OS", "Products", "Services", "Pricing", "Docs", "Start"];
const MORE_LINKS = ["Standards", "How it Works", "GitHub", "About", "Team", "News"];
// Lean links for any signed-in user — no product/pricing/team marketing clutter.
// OhhO OS + Docs stay available after sign-in (the engine + public reference).
const DEV_LINKS = ["OhhO OS", "Docs", "Link"];

export default function Nav() {
  const [scrolled, setScrolled] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const { user, subscription } = useAuth();
  const signedIn = Boolean(user);
  const subscribed = Boolean(user && hasConsoleAccess(subscription));

  useEffect(() => {
    // On mobile (< 768px) the only visible nav element is the "OhhO" wordmark
    // — every link is `hidden md:flex`. A transparent nav over bright hero
    // footage at the top of the page makes that wordmark unreadable, so we
    // force `scrolled = true` whenever the viewport is below the md break,
    // regardless of scroll position. Also re-evaluate on resize so device
    // rotation / window resizing keeps the bar persistent on phones.
    const handler = () => {
      const isMobile = window.innerWidth < 768;
      setScrolled(window.scrollY > 50 || isMobile);
      // Auto-close the mobile menu if the user crosses into desktop width so
      // we never end up with both the desktop row and a stale open hamburger
      // menu rendered simultaneously.
      if (!isMobile) setMobileOpen(false);
    };
    handler(); // set the correct state before first paint
    window.addEventListener("scroll", handler, { passive: true });
    window.addEventListener("resize", handler, { passive: true });
    return () => {
      window.removeEventListener("scroll", handler);
      window.removeEventListener("resize", handler);
    };
  }, []);

  // Closing the mobile menu on route change is a no-op here because we use
  // plain <a> navigations (full page loads); the menu re-mounts anyway.

  const navHref = (link: string): string => {
    switch (link) {
      case "No Lock-In": return NO_LOCKIN_HREF;
      case "Why OhhO": return WHY_HREF;
      case "Start": return START_HREF;
      case "OhhO OS": return OS_HREF;
      case "Standards": return STANDARDS_HREF;
      case "Products": return PRODUCTS_HREF;
      case "Services": return SERVICES_HREF;
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

  const linkColor = (link: string): string =>
    link === "OhhO OS" || link === "No Lock-In" ? "var(--cyan)" : "rgba(255,255,255,0.52)";

  const links = signedIn ? DEV_LINKS : PRIMARY_LINKS;

  return (
    <>
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
            const base = linkColor(link);
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

          {/* "More" dropdown — secondary + company links, marketing nav only.
              CSS-only: opens on hover and on keyboard focus-within (no state). */}
          {!signedIn && (
            <div className="relative group">
              <button
                type="button"
                aria-haspopup="true"
                className="inline-flex items-center gap-1 text-sm font-medium px-[13px] py-[7px] rounded-md transition-all duration-200 hover:bg-white/5 whitespace-nowrap group-focus-within:text-white"
                style={{ color: "rgba(255,255,255,0.52)" }}
              >
                More <ChevronDown size={13} strokeWidth={2} className="opacity-70 transition-transform duration-200 group-hover:rotate-180 group-focus-within:rotate-180" />
              </button>
              {/* pt-2 keeps a hover-bridge so the menu doesn't close in the gap */}
              <div className="absolute right-0 top-full pt-2 min-w-[190px] opacity-0 invisible translate-y-1 transition-all duration-200 group-hover:opacity-100 group-hover:visible group-hover:translate-y-0 group-focus-within:opacity-100 group-focus-within:visible group-focus-within:translate-y-0">
                <div
                  className="flex flex-col rounded-xl p-1.5"
                  style={{
                    background: "rgba(10,14,26,.96)",
                    border: "1px solid rgba(255,255,255,0.09)",
                    backdropFilter: "blur(20px)",
                    WebkitBackdropFilter: "blur(20px)",
                    boxShadow: "0 14px 44px rgba(0,0,0,0.45)",
                  }}
                >
                  {MORE_LINKS.map((link) => (
                    <a
                      key={link}
                      href={navHref(link)}
                      target={link === "GitHub" ? "_blank" : undefined}
                      rel={link === "GitHub" ? "noopener noreferrer" : undefined}
                      className="text-[13px] font-medium px-3 py-2 rounded-lg transition-colors duration-150 whitespace-nowrap"
                      style={{ color: "rgba(255,255,255,0.6)" }}
                      onMouseEnter={(e) => {
                        e.currentTarget.style.color = "#fff";
                        e.currentTarget.style.background = "rgba(255,255,255,0.06)";
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.color = "rgba(255,255,255,0.6)";
                        e.currentTarget.style.background = "transparent";
                      }}
                    >
                      {link}
                    </a>
                  ))}
                </div>
              </div>
            </div>
          )}

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

        {/* ── Mobile hamburger — visible only below the md breakpoint ──
            Standard pattern for the otherwise-inaccessible nav links. Closes
            on tap-outside, route change, or resize above the md breakpoint. */}
        <button
          type="button"
          aria-label={mobileOpen ? "Close menu" : "Open menu"}
          aria-expanded={mobileOpen}
          onClick={() => setMobileOpen((v) => !v)}
          className="md:hidden inline-flex items-center justify-center w-10 h-10 rounded-lg transition-colors hover:bg-white/[0.06]"
          style={{ color: "rgba(255,255,255,0.8)" }}
        >
          {mobileOpen ? <X size={20} strokeWidth={2} /> : <Menu size={20} strokeWidth={2} />}
        </button>
      </nav>

      {/* ── Mobile drop-down panel — rendered only when hamburger is open ──
          Sits *below* the 62px nav bar; sized to viewport; nothing else needs
          to shift. Includes every link the desktop row has, plus the auth + plan
          CTAs so Sign in / Console / Upgrade are reachable on a phone. */}
      {mobileOpen && (
        <MobileMenu
          links={links}
          moreLinks={!signedIn ? MORE_LINKS : []}
          navHref={navHref}
          linkColor={linkColor}
          signedIn={signedIn}
          subscribed={subscribed}
          onNavigate={() => setMobileOpen(false)}
        />
      )}
    </>
  );
}

// ── MobileMenu — the slide-down panel that opens when the hamburger is tapped.
// Rendered outside the fixed <nav> (which has a strict 62px height + padding
// that would clip a child panel); positioned independently below the bar.
function MobileMenu({
  links,
  moreLinks,
  navHref,
  linkColor,
  signedIn,
  subscribed,
  onNavigate,
}: {
  links: string[];
  moreLinks: string[];
  navHref: (link: string) => string;
  linkColor: (link: string) => string;
  signedIn: boolean;
  subscribed: boolean;
  onNavigate: () => void;
}) {
  // Lock body scroll while the menu is open so the user doesn't accidentally
  // scroll the page behind it.
  useEffect(() => {
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prev;
    };
  }, []);

  // Tap-outside (on the backdrop) closes the menu. Tap on a link closes via
  // onNavigate. Esc also closes.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onNavigate();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onNavigate]);

  return (
    <>
      {/* full-screen backdrop — transparent to taps EXCEPT on itself, so any
          tap outside the panel closes the menu without breaking the page */}
      <div
        onClick={onNavigate}
        style={{
          position: "fixed",
          inset: "62px 0 0 0",
          background: "rgba(5,7,14,0.55)",
          zIndex: 99,
        }}
      />
      {/* The panel — slides in below the nav, takes the full width. */}
      <div
        style={{
          position: "fixed",
          top: "62px",
          left: 0,
          right: 0,
          maxHeight: "calc(100vh - 62px)",
          overflowY: "auto",
          background: "rgba(10,14,26,0.98)",
          borderBottom: "1px solid rgba(255,255,255,0.08)",
          padding: "12px 16px 24px",
          zIndex: 100,
          boxShadow: "0 24px 60px rgba(0,0,0,0.6)",
        }}
      >
        {/* Primary links */}
        <div className="flex flex-col gap-1 mb-3">
          {links.map((link) => (
            <a
              key={link}
              href={navHref(link)}
              onClick={onNavigate}
              target={link === "GitHub" ? "_blank" : undefined}
              rel={link === "GitHub" ? "noopener noreferrer" : undefined}
              className="block text-[15px] font-medium px-4 py-3 rounded-lg transition-colors hover:bg-white/[0.06] whitespace-nowrap"
              style={{ color: linkColor(link) }}
            >
              {link}
            </a>
          ))}
        </div>

        {/* "More" links — flat list, not a dropdown, on mobile. */}
        {moreLinks.length > 0 && (
          <>
            <div
              style={{
                height: "1px",
                background: "rgba(255,255,255,0.08)",
                margin: "8px 4px 12px",
              }}
            />
            <div className="flex flex-col gap-1 mb-3">
              {moreLinks.map((link) => (
                <a
                  key={link}
                  href={navHref(link)}
                  onClick={onNavigate}
                  target={link === "GitHub" ? "_blank" : undefined}
                  rel={link === "GitHub" ? "noopener noreferrer" : undefined}
                  className="block text-[13px] font-medium px-4 py-2.5 rounded-lg transition-colors hover:bg-white/[0.06]"
                  style={{ color: "rgba(255,255,255,0.6)" }}
                >
                  {link}
                </a>
              ))}
            </div>
          </>
        )}

        {/* CTA buttons — mirror the desktop row: Get Started / Upgrade,
            Console, and the auth control (Sign in / avatar). Auth components
            handle their own loading state — but during loading they render
            null (see ConsoleNavButton.tsx:25 / UserMenu.tsx:30). To avoid the
            "sometimes there's no Sign in button" symptom on mobile, the
            placeholder below stays visible during loading. */}
        <div
          style={{
            height: "1px",
            background: "rgba(255,255,255,0.08)",
            margin: "12px 4px",
          }}
        />
        <div className="flex flex-col gap-2 px-1">
          {/* Plan CTA */}
          {!signedIn && (
            <a
              href="/#pricing"
              onClick={onNavigate}
              className="block text-center text-[14px] font-semibold py-3 rounded-lg"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Get Started
            </a>
          )}
          {signedIn && !subscribed && (
            <a
              href={UPGRADE_HREF}
              onClick={onNavigate}
              className="inline-flex items-center justify-center gap-1.5 text-[14px] font-semibold py-3 rounded-lg"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              <Sparkles size={14} strokeWidth={2.5} />
              Upgrade
            </a>
          )}

          {/* Console + auth row — render placeholders during auth loading so
              the user always sees *some* control here. The wrapper sits below
              the inline CTAs. */}
          <div className="flex items-center justify-between gap-3 mt-1">
            <span className="flex-1"><ConsoleNavButton /></span>
            <span><UserMenu /></span>
          </div>

          {/* Auth-loading fallback — ConsoleNavButton + UserMenu both return
              null while AuthProvider.loading is true (during Supabase session
              resolution). That leaves the row above empty on slow networks.
              Show an inline "Sign in" placeholder during loading so the bar
              never reads as "broken / missing controls." */}
          <AuthLoadingFallback />
        </div>
      </div>
    </>
  );
}

// During auth `loading`, both ConsoleNavButton and UserMenu render null. This
// tiny fallback shows a disabled "…" pill in the spot the Sign-in button will
// occupy, so the panel never looks broken while Supabase resolves.
function AuthLoadingFallback() {
  const { loading, user } = useAuth();
  if (!loading) return null;
  if (user) return null;
  return (
    <div
      className="text-center text-[12px] mt-2 font-mono"
      style={{ color: "rgba(255,255,255,0.4)" }}
    >
      Loading sign-in…
    </div>
  );
}
