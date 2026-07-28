import type { Metadata } from "next";

import { NO_INDEX } from "@/lib/seo";

/*
 * Sign-in page — noindex: no content to rank, and it is the target of the site's main CTA.
 *
 * The metadata lives here rather than in page.tsx because that page is a
 * "use client" component, and client components cannot export `metadata`.
 * This layout renders `children` unchanged — same pattern as app/team/layout.tsx.
 */
export const metadata: Metadata = {
  title: "Sign in | OhhO",
  robots: NO_INDEX,
};

export default function LoginLayout({ children }: { children: React.ReactNode }) {
  return children;
}
