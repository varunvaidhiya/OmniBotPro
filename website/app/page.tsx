"use client";

/*
 * Home — the marketing landing page.
 *
 * For a signed-in user with an active subscription, the home page IS the
 * console: they're redirected to /console so their focus stays on their
 * robots, not on product/pricing marketing. Visitors and free users see the
 * full marketing site (hero, OhhO OS umbrella, products, pricing, CTA).
 */

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";

import Nav from "@/components/Nav";
import Hero from "@/components/Hero";
import StatsBar from "@/components/StatsBar";
import OhhoOS from "@/components/OhhoOS";
import Products from "@/components/Products";
import HowItWorks from "@/components/HowItWorks";
import Pricing from "@/components/Pricing";
import CtaBanner from "@/components/CtaBanner";
import Footer from "@/components/Footer";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function Home() {
  const { loading, user, subscription } = useAuth();
  const router = useRouter();
  const subscribed = Boolean(user && hasConsoleAccess(subscription));

  useEffect(() => {
    if (!loading && subscribed) router.replace("/console");
  }, [loading, subscribed, router]);

  // Subscribed users never see marketing content — show a brief redirect shell.
  if (!loading && subscribed) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ background: "var(--bg)" }}>
        <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
          <Loader2 size={26} className="animate-spin" />
          <span className="text-[12.5px] font-mono">Opening your console…</span>
        </div>
      </div>
    );
  }

  return (
    <>
      <Nav />
      <main>
        <Hero />
        <StatsBar />
        <OhhoOS />
        <Products />
        <HowItWorks />
        <Pricing />
        <CtaBanner />
      </main>
      <Footer />
    </>
  );
}
