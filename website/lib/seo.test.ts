import { describe, it, expect } from "vitest";
import type { Metadata } from "next";

import robots from "@/app/robots";
import sitemap from "@/app/sitemap";
import { NO_INDEX, PRIVATE_ROUTES } from "@/lib/seo";

const SITE_URL = "https://ohho-robotics.com";

const rules = robots().rules as { allow?: string; disallow?: string[] };
const disallow = rules.disallow ?? [];

/** robots.txt Disallow is a prefix match against the URL path. */
const isBlocked = (path: string) => disallow.some((rule) => path.startsWith(rule));

describe("robots.txt", () => {
  it("blocks only /api/, which cannot carry a noindex tag", () => {
    // Anything that renders HTML should be kept out of the index with `noindex`
    // instead — a Disallow prevents Google from ever reading that tag, and gets
    // reported in Search Console as "Blocked by robots.txt".
    expect(disallow).toEqual(["/api/"]);
  });

  it("still allows the site root and advertises the sitemap", () => {
    expect(rules.allow).toBe("/");
    expect(robots().sitemap).toBe(`${SITE_URL}/sitemap.xml`);
  });
});

describe("robots.txt vs sitemap", () => {
  it("never blocks a URL it also asks Google to crawl", () => {
    // Submitting a URL in the sitemap while disallowing it is a direct
    // contradiction, and the single most likely way this regresses.
    const contradictions = sitemap()
      .map((entry) => new URL(entry.url).pathname)
      .filter(isBlocked);
    expect(contradictions).toEqual([]);
  });
});

describe("public product pages are crawlable", () => {
  // /link was blocked by robots.txt while its 19 sibling consoles were not —
  // the bug this suite exists to prevent. Each is a server component with its
  // own SEO metadata, so they must all be treated the same way.
  const consoles = [
    "/autonomy", "/bench", "/bridge", "/build", "/care", "/comply", "/data",
    "/fleet", "/frame", "/garage", "/link", "/market", "/mind", "/pilot",
    "/proof", "/serve", "/shield", "/train", "/twin", "/view",
  ];

  it.each(consoles)("%s is not blocked", (path) => {
    expect(isBlocked(path)).toBe(false);
  });

  it("does not block the marketing surface", () => {
    for (const path of ["/", "/why", "/start", "/docs", "/os", "/products/build", "/news/ohho-mind"]) {
      expect(isBlocked(path)).toBe(false);
    }
  });
});

describe("private routes are noindex", () => {
  it("marks NO_INDEX as neither indexable nor followable", () => {
    expect(NO_INDEX).toEqual({ index: false, follow: false });
  });

  // Imports are spelled out rather than built from the route string: Vite can
  // only analyse a dynamic import whose static prefix includes the extension,
  // and an unresolvable one here would skip the assertion instead of failing.
  const layouts: Record<(typeof PRIVATE_ROUTES)[number], () => Promise<{ metadata?: Metadata }>> = {
    "/console": () => import("@/app/console/layout"),
    "/account": () => import("@/app/account/layout"),
    "/upgrade": () => import("@/app/upgrade/layout"),
    "/login": () => import("@/app/login/layout"),
    "/auth": () => import("@/app/auth/layout"),
  };

  it("covers every private route", () => {
    expect(Object.keys(layouts).sort()).toEqual([...PRIVATE_ROUTES].sort());
  });

  it.each(PRIVATE_ROUTES)("%s has a layout exporting noindex", async (route) => {
    // A private route that loses its noindex tag becomes silently indexable,
    // which robots.txt no longer guards against.
    const mod = await layouts[route]();
    expect(mod.metadata?.robots).toEqual({ index: false, follow: false });
  });

  it.each(PRIVATE_ROUTES)("%s stays crawlable so the tag can be read", (route) => {
    expect(isBlocked(route)).toBe(false);
  });
});
