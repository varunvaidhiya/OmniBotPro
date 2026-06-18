/*
 * Supabase browser client.
 *
 * The website is a static export, so auth runs entirely client-side via
 * Supabase. Credentials come from NEXT_PUBLIC_* env vars (inlined at build).
 * When they're absent the client is null and the app degrades gracefully —
 * auth UI shows a "not configured" notice — so the site keeps building and
 * deploying with or without credentials.
 */

import { createClient, type SupabaseClient } from "@supabase/supabase-js";

const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
const anonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

export const isSupabaseConfigured = Boolean(url && anonKey);

/** Base URL for Supabase Edge Functions (Stripe checkout / portal / webhook). */
export const supabaseFunctionsUrl = url ? `${url.replace(/\/$/, "")}/functions/v1` : "";

let client: SupabaseClient | null = null;

/** Returns the singleton browser client, or null when not configured. */
export function getSupabase(): SupabaseClient | null {
  if (!isSupabaseConfigured) return null;
  if (!client) {
    client = createClient(url as string, anonKey as string, {
      auth: {
        persistSession: true,
        autoRefreshToken: true,
        detectSessionInUrl: true,
        flowType: "pkce",
      },
    });
  }
  return client;
}

/** Absolute origin for OAuth / magic-link redirects (browser only). */
export function siteOrigin(): string {
  if (typeof window !== "undefined") return window.location.origin;
  return process.env.NEXT_PUBLIC_SITE_URL ?? "";
}

export type OAuthProvider = "google";

/**
 * Which social providers to surface on the login page. Email magic-link always
 * works with just Supabase; Google requires a (free) Google Cloud OAuth
 * credential, so it stays hidden until explicitly enabled via
 * NEXT_PUBLIC_OAUTH_PROVIDERS="google".
 */
export const enabledOAuthProviders: OAuthProvider[] = (process.env.NEXT_PUBLIC_OAUTH_PROVIDERS ?? "")
  .split(",")
  .map((p) => p.trim().toLowerCase())
  .filter((p): p is OAuthProvider => p === "google");
