import type { Metadata } from "next";

import { NO_INDEX } from "@/lib/seo";

/*
 * Account and billing — noindex: per-user private data.
 *
 * The metadata lives here rather than in page.tsx because that page is a
 * "use client" component, and client components cannot export `metadata`.
 * This layout renders `children` unchanged — same pattern as app/team/layout.tsx.
 */
export const metadata: Metadata = {
  title: "Account | OhhO",
  robots: NO_INDEX,
};

export default function AccountLayout({ children }: { children: React.ReactNode }) {
  return children;
}
