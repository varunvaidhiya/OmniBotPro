import type { MetadataRoute } from "next";

const SITE_URL = "https://ohho-robotics.com";

/**
 * robots.txt — allow the public marketing + docs surface, keep the app,
 * auth and checkout routes out of the index, and point crawlers at the sitemap.
 */
export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/console", "/account", "/upgrade", "/login", "/auth/", "/link"],
    },
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
