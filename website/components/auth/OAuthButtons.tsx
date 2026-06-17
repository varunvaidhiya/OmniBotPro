"use client";

/* Google + Apple sign-in buttons (brand-styled), used on the login page. */

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { useAuth } from "@/lib/auth/AuthProvider";
import { enabledOAuthProviders, type OAuthProvider } from "@/lib/auth/supabase";

export default function OAuthButtons({ next, disabled }: { next?: string; disabled?: boolean }) {
  const { signInWithOAuth } = useAuth();
  const [busy, setBusy] = useState<OAuthProvider | null>(null);

  if (enabledOAuthProviders.length === 0) return null;

  const go = async (provider: OAuthProvider) => {
    setBusy(provider);
    const { error } = await signInWithOAuth(provider, next);
    if (error) setBusy(null); // otherwise the browser is already redirecting
  };

  return (
    <div className="flex flex-col gap-2.5">
      {enabledOAuthProviders.includes("google") && (
        <button
          onClick={() => go("google")}
          disabled={disabled || busy !== null}
          className="flex items-center justify-center gap-2.5 w-full py-2.5 rounded-lg text-[13.5px] font-semibold transition-colors disabled:opacity-50"
          style={{ background: "#fff", color: "#1f1f1f" }}
        >
          {busy === "google" ? <Loader2 size={16} className="animate-spin" /> : <GoogleIcon />}
          Continue with Google
        </button>
      )}
      {enabledOAuthProviders.includes("apple") && (
        <button
          onClick={() => go("apple")}
          disabled={disabled || busy !== null}
          className="flex items-center justify-center gap-2.5 w-full py-2.5 rounded-lg text-[13.5px] font-semibold transition-colors disabled:opacity-50"
          style={{ background: "#000", color: "#fff", border: "1px solid rgba(255,255,255,.2)" }}
        >
          {busy === "apple" ? <Loader2 size={16} className="animate-spin" /> : <AppleIcon />}
          Continue with Apple
        </button>
      )}
    </div>
  );
}

function GoogleIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 48 48" aria-hidden="true">
      <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" />
      <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" />
      <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" />
      <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" />
    </svg>
  );
}

function AppleIcon() {
  return (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M17.05 12.04c-.03-2.6 2.12-3.85 2.22-3.91-1.21-1.77-3.09-2.01-3.76-2.04-1.6-.16-3.12.94-3.93.94-.81 0-2.06-.92-3.39-.9-1.74.03-3.35 1.01-4.25 2.57-1.81 3.14-.46 7.79 1.3 10.34.86 1.25 1.89 2.65 3.23 2.6 1.3-.05 1.79-.84 3.36-.84 1.57 0 2.01.84 3.39.81 1.4-.02 2.29-1.27 3.15-2.53.99-1.45 1.4-2.86 1.42-2.93-.03-.01-2.72-1.04-2.75-4.13zM14.53 3.6c.71-.87 1.19-2.07 1.06-3.27-1.02.04-2.27.68-3.01 1.54-.66.76-1.24 1.99-1.08 3.16 1.14.09 2.31-.58 3.03-1.43z" />
    </svg>
  );
}
