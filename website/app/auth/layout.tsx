import type { Metadata } from "next";

import { NO_INDEX } from "@/lib/seo";

/*
 * OAuth / magic-link return URLs — noindex: one-time callback URLs carrying tokens.
 *
 * The metadata lives here rather than in page.tsx because that page is a
 * "use client" component, and client components cannot export `metadata`.
 * This layout renders `children` unchanged — same pattern as app/team/layout.tsx.
 */
export const metadata: Metadata = {
  title: "Signing in… | OhhO",
  robots: NO_INDEX,
};

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return children;
}
