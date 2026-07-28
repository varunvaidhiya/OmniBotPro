import type { MetadataRoute } from "next";

const SITE_URL = "https://ohho-robotics.com";

/**
 * robots.txt — crawl rules only.
 *
 * This file decides what may be *crawled*, not what may be *indexed*. Keeping a
 * page out of search results is the job of a `noindex` meta tag (see `NO_INDEX`
 * in lib/seo.ts), and the two must not be combined: Google can only read
 * `noindex` on a page it is allowed to fetch, so a `Disallow` actively prevents
 * the deindexing it looks like it is asking for.
 *
 * That mix-up is why Search Console reported "Blocked by robots.txt" against
 * /console, /account, /upgrade, /login, /auth/ and /link. The first five are
 * private app routes that do belong out of the index — they now carry `noindex`
 * and are crawlable, so Google can see the tag and drop them properly. /link
 * was simply a mistake: it is a public product page with its own SEO metadata,
 * identical in shape to the other product consoles, none of which were blocked.
 *
 * `/api/` is the one legitimate `Disallow` left. Those routes return JSON, not
 * HTML, so there is nowhere to put a meta tag and a crawl block is the only
 * mechanism available. Nothing links to them, so they won't be reported as
 * blocked the way the pages above were.
 */
export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/api/"],
    },
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
