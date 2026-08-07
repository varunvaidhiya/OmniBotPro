import type { MetadataRoute } from "next";
import { PRODUCTS } from "@/lib/products";
import { getAllDocs } from "@/lib/docs";
import { SITE_URL } from "@/lib/seo";

/*
 * Sitemap for search engines.
 *
 * Contains every publicly indexable URL and nothing else. Signed-in product
 * consoles (/build, /fleet, /view, …), auth and checkout are deliberately
 * absent — they are noindex (see lib/seo#NO_INDEX), and listing a noindex URL
 * in a sitemap is a contradictory signal that costs crawl budget.
 *
 * `lastModified` is deliberately NOT `new Date()`. A sitemap regenerated on
 * every deploy that claims every page changed teaches Google to distrust the
 * field and ignore it; pages carry the date their content actually last moved.
 */

/** Bump a page's date when you meaningfully change its content. */
const LAST_CONTENT_UPDATE = "2026-07-15";

interface Entry {
  path: string;
  priority: number;
  changeFrequency: "daily" | "weekly" | "monthly";
  lastModified?: string;
}

const MARKETING: Entry[] = [
  { path: "", priority: 1.0, changeFrequency: "weekly" },
  { path: "/why", priority: 0.9, changeFrequency: "monthly" },
  { path: "/start", priority: 0.9, changeFrequency: "monthly" },
  { path: "/os", priority: 0.8, changeFrequency: "monthly" },
  { path: "/services", priority: 0.8, changeFrequency: "monthly" },
  { path: "/for-builders", priority: 0.8, changeFrequency: "monthly" },
  { path: "/docs", priority: 0.7, changeFrequency: "weekly" },
  { path: "/standards", priority: 0.7, changeFrequency: "monthly" },
  { path: "/about", priority: 0.6, changeFrequency: "monthly" },
  { path: "/team", priority: 0.5, changeFrequency: "monthly" },
];

/** Announcement posts. Add new posts here when a page lands under /news. */
const NEWS: Entry[] = [
  { path: "/news/ohho-mind", priority: 0.6, changeFrequency: "monthly" },
  { path: "/news/omnivla-engine", priority: 0.6, changeFrequency: "monthly" },
];

export default function sitemap(): MetadataRoute.Sitemap {
  const entry = (e: Entry): MetadataRoute.Sitemap[number] => ({
    url: `${SITE_URL}${e.path}`,
    lastModified: new Date(e.lastModified ?? LAST_CONTENT_UPDATE),
    changeFrequency: e.changeFrequency,
    priority: e.priority,
  });

  // One marketing page per product — these are the indexable counterparts of
  // the noindex consoles that share their slug (/products/build vs /build).
  const products: MetadataRoute.Sitemap = PRODUCTS.map((p) => ({
    url: `${SITE_URL}/products/${p.slug}`,
    lastModified: new Date(LAST_CONTENT_UPDATE),
    changeFrequency: "monthly" as const,
    priority: 0.7,
  }));

  const docs: MetadataRoute.Sitemap = getAllDocs().map((doc) => ({
    url: `${SITE_URL}/docs/${doc.slug.join("/")}`,
    lastModified: new Date(LAST_CONTENT_UPDATE),
    changeFrequency: "monthly" as const,
    priority: 0.5,
  }));

  return [...MARKETING.map(entry), ...NEWS.map(entry), ...products, ...docs];
}
