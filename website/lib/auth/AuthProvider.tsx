"use client";

/*
 * AuthProvider — client-side auth + subscription context.
 *
 * Wraps the whole app (in app/layout.tsx). Exposes the current user, their
 * subscription row, and sign-in/out helpers. Subscription state is read from
 * the `subscriptions` table (kept current by the Stripe webhook Edge Function).
 * When Supabase isn't configured everything no-ops and `configured` is false.
 */

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { Session, User } from "@supabase/supabase-js";

import { getSupabase, isSupabaseConfigured, siteOrigin, type OAuthProvider } from "./supabase";
import type { Subscription } from "./plans";

interface AuthContextValue {
  configured: boolean;
  loading: boolean;
  user: User | null;
  session: Session | null;
  subscription: Subscription | null;
  signInWithEmail: (email: string, next?: string) => Promise<{ error?: string; sent?: boolean }>;
  signInWithOAuth: (provider: OAuthProvider, next?: string) => Promise<{ error?: string }>;
  signOut: () => Promise<void>;
  refreshSubscription: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function safeNextPath(next?: string | null): string {
  if (!next) return "/#products";
  try {
    const url = new URL(next, siteOrigin() || "http://localhost");
    if (url.origin !== (siteOrigin() || url.origin)) return "/#products";
    return `${url.pathname}${url.search}${url.hash}` || "/#products";
  } catch {
    return next.startsWith("/") && !next.startsWith("//") ? next : "/#products";
  }
}

function callbackUrl(next?: string): string {
  const origin = siteOrigin();
  const n = encodeURIComponent(safeNextPath(next));
  return `${origin}/auth/callback?next=${n}`;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const supabase = getSupabase();
  const [loading, setLoading] = useState(isSupabaseConfigured);
  const [session, setSession] = useState<Session | null>(null);
  const [subscription, setSubscription] = useState<Subscription | null>(null);

  const loadSubscription = useCallback(
    async (userId: string) => {
      if (!supabase) return;
      const { data } = await supabase
        .from("subscriptions")
        .select("plan, status, current_period_end, cancel_at_period_end")
        .eq("user_id", userId)
        .maybeSingle();
      setSubscription((data as Subscription) ?? null);
    },
    [supabase],
  );

  useEffect(() => {
    if (!supabase) {
      setLoading(false);
      return;
    }
    let active = true;
    supabase.auth.getSession().then(({ data }) => {
      if (!active) return;
      setSession(data.session);
      if (data.session) loadSubscription(data.session.user.id);
      setLoading(false);
    });
    const { data: sub } = supabase.auth.onAuthStateChange((_event, s) => {
      setSession(s);
      if (s) loadSubscription(s.user.id);
      else setSubscription(null);
    });
    return () => {
      active = false;
      sub.subscription.unsubscribe();
    };
  }, [supabase, loadSubscription]);

  const signInWithEmail = useCallback(
    async (email: string, next?: string) => {
      if (!supabase) return { error: "Authentication isn't configured yet." };
      const { error } = await supabase.auth.signInWithOtp({
        email,
        options: { emailRedirectTo: callbackUrl(next) },
      });
      return error ? { error: error.message } : { sent: true };
    },
    [supabase],
  );

  const signInWithOAuth = useCallback(
    async (provider: OAuthProvider, next?: string) => {
      if (!supabase) return { error: "Authentication isn't configured yet." };
      const { error } = await supabase.auth.signInWithOAuth({
        provider,
        options: { redirectTo: callbackUrl(next) },
      });
      return error ? { error: error.message } : {};
    },
    [supabase],
  );

  const signOut = useCallback(async () => {
    await supabase?.auth.signOut();
    setSubscription(null);
  }, [supabase]);

  const refreshSubscription = useCallback(async () => {
    if (session) await loadSubscription(session.user.id);
  }, [session, loadSubscription]);

  const value: AuthContextValue = {
    configured: isSupabaseConfigured,
    loading,
    user: session?.user ?? null,
    session,
    subscription,
    signInWithEmail,
    signInWithOAuth,
    signOut,
    refreshSubscription,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within <AuthProvider>");
  return ctx;
}
