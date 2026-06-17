"use client";

/* Google sign-in button (brand-styled), used on the login page. */

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { useAuth } from "@/lib/auth/AuthProvider";
import { enabledOAuthProviders } from "@/lib/auth/supabase";

export default function OAuthButtons({ next, disabled }: { next?: string; disabled?: boolean }) {
  const { signInWithOAuth } = useAuth();
  const [busy, setBusy] = useState(false);

  if (!enabledOAuthProviders.includes("google")) return null;

  const go = async () => {
    setBusy(true);
    const { error } = await signInWithOAuth("google", next);
    if (error) setBusy(false); // otherwise the browser is already redirecting
  };

  return (
    <button
      onClick={go}
      disabled={disabled || busy}
      className="flex items-center justify-center gap-2.5 w-full py-2.5 rounded-lg text-[13.5px] font-semibold transition-colors disabled:opacity-50"
      style={{ background: "#fff", color: "#1f1f1f" }}
    >
      {busy ? <Loader2 size={16} className="animate-spin" /> : <GoogleIcon />}
      Continue with Google
    </button>
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
