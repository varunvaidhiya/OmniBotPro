/*
 * Central place for the site's "where does this button go" links, so the
 * purchase / get-started path is defined once and easy to change.
 *
 * Self-serve checkout is live: paid plans (Builder, Fleet) go through Stripe
 * Checkout via the /upgrade flow. Free sign-up goes to /login. Only sales-led
 * actions (Forge / enterprise) and the Contact link use a pre-filled email.
 */

export const CONTACT_EMAIL = "varun.vaidhiya@gmail.com";

export const PRICING_HREF = "/#pricing";
export const PRODUCTS_HREF = "/#products";
/** Homepage anchor for the "No vendor lock-in / brand-agnostic" moat section. */
export const NO_LOCKIN_HREF = "/#no-lock-in";
/** Homepage anchor for the "single umbrella / full lifecycle" moat section. */
export const LIFECYCLE_HREF = "/#lifecycle";

/** Free sign-up (no payment) — Spark plan / "Start free" CTAs. */
export const SIGNUP_HREF = "/login?next=/garage";
/** Self-serve paid checkout (Builder / Fleet) — gated by sign-in inside /upgrade. */
export const UPGRADE_HREF = "/upgrade";

/** Internal documentation hub (app/docs) — renders Markdown from website/docs. */
export const DOCS_HREF = "/docs";
/** OhhO OS — the open robot engine that powers every product (marketing hub). */
export const OS_HREF = "/os";
/** Standards — every industry-standard protocol OhhO supports (marketing hub). */
export const STANDARDS_HREF = "/standards";
/** Why OhhO — the wedge-vs-moat positioning page (openness is the wedge, data is the moat). */
export const WHY_HREF = "/why";
/** Quickstart — install the open engine and drive a robot in 5 minutes. */
export const START_HREF = "/start";
export const GITHUB_HREF = "https://github.com/varunvaidhiya/OmniBotPro";
export const TWITTER_HREF = "https://x.com/ohho_ai";
export const LINKEDIN_HREF = "https://linkedin.com/company/ohho-ai";

/**
 * Optional "Buy me a coffee" support link for this open-source project.
 * Update the slug to your own Buy Me a Coffee / Ko-fi / GitHub Sponsors page.
 */
export const BUYMEACOFFEE_HREF = "https://www.buymeacoffee.com/varunvaidhiya";

/** mailto: link with a pre-filled subject (used by Get Started / plan CTAs). */
export function contactMailto(subject: string): string {
  return `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}`;
}

/** Detail-page route for a product slug. */
export function productHref(slug: string): string {
  return `/products/${slug}`;
}
