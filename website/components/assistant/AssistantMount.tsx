"use client";

/*
 * AssistantMount — decides where the AI assistant appears.
 *
 * Mounted once in the root layout. It shows the AssistantWidget (a single,
 * consistent launcher in the same bottom-right spot) on every product console
 * and the console hub, and nowhere else — so a signed-in operator always finds
 * the AI in the same place, while marketing pages stay clean.
 *
 * The current route's first path segment is passed to the widget as the
 * "product", which both focuses the assistant's tools and labels the panel.
 */

import { usePathname } from "next/navigation";

import AssistantWidget from "./AssistantWidget";
import { useAuth } from "@/lib/auth/AuthProvider";
import { PRODUCTS } from "@/lib/products";

// Route segment → console display name, derived from the product catalog so it
// stays in sync as products are added. (A product's console lives at app.href,
// e.g. OhhO Connect's console is /garage — keyed by the path segment here.)
const SEGMENT_LABEL: Record<string, string> = (() => {
  const map: Record<string, string> = { console: "OhhO Console" };
  for (const p of PRODUCTS) {
    const href = p.app?.href;
    if (href) map[href.replace(/^\//, "")] = p.name;
  }
  return map;
})();

const CONSOLE_SEGMENTS = new Set(Object.keys(SEGMENT_LABEL));

export default function AssistantMount() {
  const pathname = usePathname() || "/";
  const { user } = useAuth();

  const segment = pathname.split("/")[1] || "";
  if (!CONSOLE_SEGMENTS.has(segment)) return null;
  // Consoles require sign-in; don't flash the launcher on the auth redirect.
  if (!user) return null;

  return <AssistantWidget product={segment} productLabel={SEGMENT_LABEL[segment] ?? segment} />;
}
