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
