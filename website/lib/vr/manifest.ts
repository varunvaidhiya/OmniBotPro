/*
 * OhhO VR manifest — the single static contract the VR app (vr_app/) reads to
 * mirror the website inside an OpenXR mixed-reality headset.
 *
 * Why a static manifest (and not a server API)?
 *   The website ships as a static export served on Vercel, and auth is
 *   client-side Supabase. There is no runtime server for the headset to call.
 *   So the VR app fetches ONE static JSON — public/vr/manifest.json — generated
 *   from this module (committed snapshot; `npm run generate:vr` to refresh), and
 *   talks to Supabase directly with the same public anon key the website inlines.
 *
 * What the headset gets from the manifest:
 *   - theme   → the OhhO design tokens (colours + fonts) so VR matches the site.
 *   - auth    → Supabase URL + public anon key + flow, for in-headset sign-in.
 *   - products→ ONLY the products that require a VR headset (Product.vr === true).
 *               Today that's Pilot (teleoperation); more will appear over time.
 *
 * Pure data + types only — deterministic (no timestamps, no env reads) so the
 * committed public/vr/manifest.json can be drift-checked in manifest.test.ts.
 */

import { PRODUCTS, type Product, type Accent, type Category } from "@/lib/products";

// ── Design tokens (mirrors the :root variables in app/globals.css) ───────────

export interface VrThemeTokens {
  bg: string;
  surface: string;
  cyan: string;
  violet: string;
  violetLite: string;
  text: string;
  muted: string;
  border: string;
  fonts: { display: string; body: string; mono: string };
}

export const VR_THEME: VrThemeTokens = {
  bg: "#0A0E1A",
  surface: "#0F1628",
  cyan: "#00D4FF",
  violet: "#7C3AED",
  violetLite: "#A78BFA",
  text: "#FFFFFF",
  muted: "rgba(255,255,255,0.52)",
  border: "rgba(255,255,255,0.07)",
  fonts: { display: "Space Grotesk", body: "Inter", mono: "JetBrains Mono" },
};

// ── Supabase auth config (public — same values vercel.json inlines as
//    NEXT_PUBLIC_* and the static site already exposes to every browser) ──────

export interface VrAuthConfig {
  provider: "supabase";
  /** Supabase project URL — GoTrue lives at `${url}/auth/v1`. */
  url: string;
  /** Public anon key (safe to embed; identical to the website's). */
  anonKey: string;
  /**
   * Sign-in method the headset should use. Email OTP (6-digit code) is the
   * headset-friendly equivalent of the website's magic link — same GoTrue
   * backend, but the user types a short code instead of opening a browser link.
   */
  signIn: "email_otp";
  /** PostgREST table holding a user's saved robots (garage). */
  garageTable: "user_robots";
}

export const VR_AUTH: VrAuthConfig = {
  provider: "supabase",
  url: "https://ucnyuappgtcpolqdaovv.supabase.co",
  anonKey:
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVjbnl1YXBwZ3RjcG9scWRhb3Z2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODE3MjU5NTEsImV4cCI6MjA5NzMwMTk1MX0.Cf3g2nxHtaeGU59UwtCdpuNwQV6cCZ4yvdBSqhAEtls",
  signIn: "email_otp",
  garageTable: "user_robots",
};

// ── VR product (serializable subset of Product — drops the JSX `icon`) ───────

export interface VrProductSpec {
  label: string;
  value: string;
}

export interface VrProduct {
  slug: string;
  name: string;
  tag: string;
  desc: string;
  accent: Accent;
  category: Category;
  hero: string;
  highlights: string[];
  specs: VrProductSpec[];
  /** In-headset launch target. For Pilot this is the teleoperation cockpit. */
  app?: { href: string; label: string };
}

/** The products that require a VR headset, in catalog order. */
export function vrProducts(source: readonly Product[] = PRODUCTS): VrProduct[] {
  return source
    .filter((p) => p.vr === true)
    .map((p) => ({
      slug: p.slug,
      name: p.name,
      tag: p.tag,
      desc: p.desc,
      accent: p.accent,
      category: p.category,
      hero: p.hero,
      highlights: p.highlights,
      specs: p.specs,
      ...(p.app ? { app: p.app } : {}),
    }));
}

// ── The manifest ─────────────────────────────────────────────────────────────

/** Schema version — bump when the manifest shape changes (VR app reads it). */
export const VR_MANIFEST_VERSION = 1 as const;

export interface VrManifest {
  version: typeof VR_MANIFEST_VERSION;
  theme: VrThemeTokens;
  auth: VrAuthConfig;
  products: VrProduct[];
}

/** Build the full manifest. Deterministic: same input → byte-identical output. */
export function buildVrManifest(source: readonly Product[] = PRODUCTS): VrManifest {
  return {
    version: VR_MANIFEST_VERSION,
    theme: VR_THEME,
    auth: VR_AUTH,
    products: vrProducts(source),
  };
}
