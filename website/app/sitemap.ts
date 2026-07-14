import type { MetadataRoute } from "next";
import { PRODUCTS } from "@/lib/products";

const SITE_URL = "https://ohho-robotics.com";

/**
 * Sitemap for search engines. Static marketing routes + every product detail
 * page (kept in sync with the product catalog automatically). Console / auth /
 * checkout routes are intentionally excluded (see robots.ts).
 */
export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();

  const pages: { path: string; priority: number; changeFrequency: "weekly" | "monthly" }[] = [
    { path: "", priority: 1.0, changeFrequency: "weekly" },
    { path: "/why", priority: 0.9, changeFrequency: "weekly" },
    { path: "/start", priority: 0.9, changeFrequency: "weekly" },
    { path: "/services", priority: 0.8, changeFrequency: "weekly" },
    { path: "/for-builders", priority: 0.8, changeFrequency: "weekly" },
    { path: "/os", priority: 0.7, changeFrequency: "monthly" },
    { path: "/standards", priority: 0.7, changeFrequency: "monthly" },
    { path: "/docs", priority: 0.7, changeFrequency: "weekly" },
    { path: "/about", priority: 0.5, changeFrequency: "monthly" },
    { path: "/team", priority: 0.5, changeFrequency: "monthly" },
  ];

  const staticEntries: MetadataRoute.Sitemap = pages.map((p) => ({
    url: `${SITE_URL}${p.path}`,
    lastModified: now,
    changeFrequency: p.changeFrequency,
    priority: p.priority,
  }));

  const productEntries: MetadataRoute.Sitemap = PRODUCTS.map((p) => ({
    url: `${SITE_URL}/products/${p.slug}`,
    lastModified: now,
    changeFrequency: "monthly" as const,
    priority: 0.6,
  }));

  return [...staticEntries, ...productEntries];
}
