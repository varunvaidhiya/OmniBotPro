import type { Metadata } from "next";

import { NO_INDEX } from "@/lib/seo";

/*
 * Checkout flow — noindex: pricing lives on the homepage; this is a transaction step.
 *
 * The metadata lives here rather than in page.tsx because that page is a
 * "use client" component, and client components cannot export `metadata`.
 * This layout renders `children` unchanged — same pattern as app/team/layout.tsx.
 */
export const metadata: Metadata = {
  title: "Upgrade | OhhO",
  robots: NO_INDEX,
};

export default function UpgradeLayout({ children }: { children: React.ReactNode }) {
  return children;
}
