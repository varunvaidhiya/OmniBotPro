import type { Metadata } from "next";

import { NO_INDEX } from "@/lib/seo";

/*
 * Signed-in console hub — noindex: private app surface, not a landing page.
 *
 * The metadata lives here rather than in page.tsx because that page is a
 * "use client" component, and client components cannot export `metadata`.
 * This layout renders `children` unchanged — same pattern as app/team/layout.tsx.
 */
export const metadata: Metadata = {
  title: "Console | OhhO",
  robots: NO_INDEX,
};

export default function ConsoleLayout({ children }: { children: React.ReactNode }) {
  return children;
}
