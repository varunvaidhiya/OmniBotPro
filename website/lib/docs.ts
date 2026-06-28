/*
 * Build-time documentation loader.
 *
 * Reads every Markdown file under `website/docs/` (recursively) and turns it
 * into a rendered HTML doc with a route under `/docs/...`. This is what powers
 * the site's Docs tab.
 *
 * Add a new `.md` file anywhere under `website/docs/` and it shows up on the
 * site automatically on the next build/deploy — no code change needed. A file
 * at `docs/products/build.md` is served at `/docs/products/build`; a folder's
 * `README.md` becomes that folder's index (e.g. `docs/products/README.md` →
 * `/docs/products`).
 *
 * Uses `fs`, so it must only be imported from Server Components / build-time
 * code (the App Router pages here are server components, so that's fine).
 */

import fs from "node:fs";
import path from "node:path";
import { Marked } from "marked";

const DOCS_ROOT = path.join(process.cwd(), "docs");
const REPO_ROOT = path.resolve(process.cwd(), "..");
const REPO_BLOB = "https://github.com/varunvaidhiya/OmniBotPro/blob/main";

export interface DocMeta {
  /** Path segments under /docs, e.g. ["products", "build"]. */
  slug: string[];
  /** Route on the site, e.g. "/docs/products/build". */
  href: string;
  title: string;
  description: string;
  /** Top-level folder, humanized (e.g. "products" → "Products"). */
  category: string;
  /** Raw top-level folder key (e.g. "products"), or "" at the docs root. */
  categoryKey: string;
  /** True when this doc is a folder's README/index. */
  isIndex: boolean;
}

export interface Doc extends DocMeta {
  html: string;
}

export interface DocGroup {
  category: string;
  categoryKey: string;
  /** The folder index (README), if one exists. */
  index?: DocMeta;
  /** Non-index docs in this group. */
  docs: DocMeta[];
}

// ── Filesystem walk ──────────────────────────────────────────────────────────

function walk(dir: string): string[] {
  if (!fs.existsSync(dir)) return [];
  const out: string[] = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (entry.isFile() && entry.name.toLowerCase().endsWith(".md")) out.push(full);
  }
  return out;
}

function fileToSlug(absPath: string): { slug: string[]; isIndex: boolean } {
  const rel = path.relative(DOCS_ROOT, absPath).replace(/\\/g, "/");
  const noExt = rel.replace(/\.md$/i, "");
  const parts = noExt.split("/").filter(Boolean);
  const base = parts[parts.length - 1] ?? "";
  if (base.toLowerCase() === "readme") {
    return { slug: parts.slice(0, -1), isIndex: true };
  }
  return { slug: parts, isIndex: false };
}

// Brand / acronym tokens that shouldn't be naively title-cased — e.g. the
// `ohho-os` docs folder should read "OhhO OS", not "Ohho Os".
const HUMANIZE_OVERRIDES: Record<string, string> = {
  ohho: "OhhO",
  os: "OS",
  ai: "AI",
  api: "API",
  ros: "ROS",
  vla: "VLA",
  rl: "RL",
  sdk: "SDK",
};

function humanize(key: string): string {
  if (!key) return "General";
  return key
    .split(/[-_]/g)
    .map((w) => HUMANIZE_OVERRIDES[w.toLowerCase()] ?? w.charAt(0).toUpperCase() + w.slice(1))
    .join(" ");
}

// ── Markdown parsing ──────────────────────────────────────────────────────────

function isWithin(child: string, parent: string): boolean {
  return child === parent || child.startsWith(parent + path.sep);
}

/** Rewrite a relative link so it points at a real site route or GitHub. */
function rewriteHref(href: string, docDir: string): string {
  if (!href) return href;
  // Leave absolute URLs, anchors, mailto and site-absolute links alone.
  if (/^(https?:|mailto:|tel:|#|\/)/i.test(href)) return href;

  const [pathPart, hashPart] = href.split("#");
  const resolved = path.resolve(docDir, pathPart);

  // A link to another doc → its /docs route.
  if (isWithin(resolved, DOCS_ROOT) && /\.md$/i.test(resolved)) {
    const { slug } = fileToSlug(resolved);
    const route = "/docs" + (slug.length ? "/" + slug.join("/") : "");
    return hashPart ? `${route}#${hashPart}` : route;
  }

  // A link to a source file inside the repo → GitHub blob view.
  if (isWithin(resolved, REPO_ROOT)) {
    const relRepo = path.relative(REPO_ROOT, resolved).replace(/\\/g, "/");
    if (!relRepo.startsWith("..")) return `${REPO_BLOB}/${relRepo}`;
  }

  return href;
}

function renderMarkdown(markdown: string, docDir: string): string {
  const md = new Marked({ gfm: true });
  md.use({
    walkTokens(token) {
      // marked link/image tokens carry an `href` we can rewrite in place.
      if ((token.type === "link" || token.type === "image") && "href" in token) {
        (token as { href: string }).href = rewriteHref((token as { href: string }).href, docDir);
      }
    },
  });
  return md.parse(markdown, { async: false }) as string;
}

// ── Metadata extraction ───────────────────────────────────────────────────────

function extractTitle(markdown: string, fallbackSlug: string[]): string {
  const m = markdown.match(/^#\s+(.+?)\s*$/m);
  if (m) return m[1].replace(/[#*`]/g, "").trim();
  const last = fallbackSlug[fallbackSlug.length - 1] ?? "Docs";
  return humanize(last);
}

function extractDescription(markdown: string): string {
  const lines = markdown.split(/\r?\n/);
  let seenTitle = false;
  for (const raw of lines) {
    const line = raw.trim();
    if (!line) continue;
    if (line.startsWith("# ")) { seenTitle = true; continue; }
    if (!seenTitle) continue;
    // Skip other headings, list items, tables, code fences, rules.
    if (/^(#{1,6}\s|[-*+]\s|\d+\.\s|\||```|---|===)/.test(line)) continue;
    const cleaned = line
      .replace(/^>\s?/, "")
      .replace(/\*\*/g, "")
      .replace(/[*`]/g, "")
      .trim();
    if (cleaned) return cleaned.length > 200 ? cleaned.slice(0, 197) + "…" : cleaned;
  }
  return "";
}

// ── Public API ────────────────────────────────────────────────────────────────

function loadAll(): Doc[] {
  const files = walk(DOCS_ROOT);
  const docs: Doc[] = files.map((abs) => {
    const markdown = fs.readFileSync(abs, "utf8");
    const { slug, isIndex } = fileToSlug(abs);
    // Group by top-level folder. A folder README (index) belongs to its own
    // folder; a nested file belongs to its first path segment.
    const key = slug.length > 1 ? slug[0] : isIndex ? slug[0] ?? "" : "";
    const html = renderMarkdown(markdown, path.dirname(abs));
    return {
      slug,
      href: "/docs" + (slug.length ? "/" + slug.join("/") : ""),
      title: extractTitle(markdown, slug),
      description: extractDescription(markdown),
      category: humanize(key),
      categoryKey: key,
      isIndex,
      html,
    };
  });
  // Stable, readable ordering: by category, indexes first, then title.
  return docs.sort((a, b) => {
    if (a.categoryKey !== b.categoryKey) return a.categoryKey.localeCompare(b.categoryKey);
    if (a.isIndex !== b.isIndex) return a.isIndex ? -1 : 1;
    return a.title.localeCompare(b.title);
  });
}

/** All docs that have a non-empty slug (i.e. are addressable under /docs/...). */
export function getAllDocs(): Doc[] {
  return loadAll().filter((d) => d.slug.length > 0);
}

export function getDocBySlug(slug: string[]): Doc | undefined {
  const target = slug.join("/");
  return loadAll().find((d) => d.slug.join("/") === target);
}

/** Docs grouped by top-level folder, for the /docs index page. */
export function getDocGroups(): DocGroup[] {
  const docs = getAllDocs();
  const byKey = new Map<string, DocGroup>();
  for (const d of docs) {
    let g = byKey.get(d.categoryKey);
    if (!g) {
      g = { category: d.category, categoryKey: d.categoryKey, docs: [] };
      byKey.set(d.categoryKey, g);
    }
    if (d.isIndex) g.index = d;
    else g.docs.push(d);
  }
  return Array.from(byKey.values()).sort((a, b) => a.categoryKey.localeCompare(b.categoryKey));
}
