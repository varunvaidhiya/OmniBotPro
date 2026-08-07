import type { Metadata } from "next";

/*
 * Shared SEO/indexing helpers.
 *
 * The important distinction this file encodes:
 *
 *   robots.txt `Disallow`  → "don't CRAWL this"   (app/robots.ts)
 *   `noindex` meta tag     → "don't INDEX this"   (NO_INDEX below)
 *
 * They are not interchangeable. A `Disallow`ed URL can still end up in Google's
 * index as a bare URL with no title or snippet if anything links to it, because
 * Google honours the crawl block but has no way to read a `noindex` it isn't
 * allowed to fetch. Worse, Search Console reports every such URL under
 * "Blocked by robots.txt", which is what prompted this file.
 *
 * So: private routes are crawlable and carry `noindex`, which is what actually
 * keeps them out of search results. `Disallow` is reserved for endpoints that
 * return no HTML and therefore cannot carry a meta tag at all (see app/robots.ts).
 *
 * The second half of this file covers canonical URLs. Every indexable page must
 * declare one: without it Google groups near-identical pages by itself and
 * reports "Duplicate without user-selected canonical", and the page is crawled
 * but never served.
 */

/**
 * Metadata `robots` value for pages that must never appear in search results.
 *
 * Emits `<meta name="robots" content="noindex, nofollow">`. `follow: false`
 * because every outbound link on these routes (nav, footer) is already reachable
 * from indexable pages, so there is nothing to gain by crawling them again.
 */
export const NO_INDEX = { index: false, follow: false } as const;

/**
 * Routes kept out of the index, each via a `layout.tsx` exporting `NO_INDEX`.
 *
 * A layout is used rather than the page itself because all of these pages are
 * `"use client"` components, and a client component cannot export `metadata`.
 * The layout is a server component that renders `children` unchanged, which is
 * the same pattern already used by app/team/layout.tsx.
 *
 * Kept here so lib/seo.test.ts can assert that each one really is noindex —
 * losing the tag would silently make an app route indexable.
 */
export const PRIVATE_ROUTES = [
  "/console",
  "/account",
  "/upgrade",
  "/login",
  "/auth",
] as const;

/**
 * Signed-in product consoles. Each is wrapped in ConsoleGate (or AuthGate for
 * /garage), so a logged-out crawler receives the same "checking access…" shell
 * followed by a client-side redirect to /login — no product content at all.
 *
 * They carry `NO_INDEX` on the page itself (these are server components, so no
 * layout is needed) and are excluded from the sitemap. Their indexable
 * counterparts are the marketing pages at /products/{slug}, which share the
 * slug: without this split Google grouped /build with /products/build and
 * repeatedly chose the console.
 *
 * They stay crawlable in robots.txt for the reason documented above.
 */
export const CONSOLE_ROUTES = [
  "/autonomy", "/bench", "/bridge", "/build", "/care", "/comply", "/data",
  "/fleet", "/frame", "/garage", "/link", "/market", "/mind", "/pilot",
  "/proof", "/serve", "/shield", "/train", "/twin", "/view",
] as const;

export const SITE_URL = "https://ohho-robotics.com";

/** Legal/brand name. The domain is ohho-robotics.com, so "OhhO Robotics" is the
 *  string people actually type — it must appear in titles and structured data. */
export const SITE_NAME = "OhhO Robotics";
export const SITE_SHORT_NAME = "OhhO";

export const DEFAULT_DESCRIPTION =
  "OhhO Robotics is the open-source robotics platform. Built for open-source robots and hardware first — and works with any brand that keeps its software layer open. Build, train, simulate, deploy, manage and regulate any robot. No vendor lock-in, ever.";

/** 1200×630 PNG generated at build time by app/opengraph-image.tsx. */
export const OG_IMAGE = "/opengraph-image";

/** Absolute URL for a site-relative path. Google prefers absolute canonicals. */
export function absoluteUrl(path: string): string {
  if (!path || path === "/") return SITE_URL;
  return `${SITE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

interface PageSeoInput {
  /** Route path, e.g. "/why". Use "/" for the homepage. */
  path: string;
  title: string;
  description: string;
  /** Set for article-style pages (news posts) so Open Graph types them right. */
  type?: "website" | "article";
  publishedTime?: string;
}

/**
 * Build the metadata for a public, indexable page — canonical, Open Graph and
 * Twitter card all derived from one path so they can never drift apart.
 */
export function pageSeo({
  path,
  title,
  description,
  type = "website",
  publishedTime,
}: PageSeoInput): Metadata {
  const url = absoluteUrl(path);
  return {
    title,
    description,
    alternates: { canonical: url },
    openGraph: {
      title,
      description,
      url,
      siteName: SITE_NAME,
      type,
      locale: "en_US",
      images: [{ url: OG_IMAGE, width: 1200, height: 630, alt: SITE_NAME }],
      ...(publishedTime ? { publishedTime } : {}),
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [OG_IMAGE],
    },
  };
}
