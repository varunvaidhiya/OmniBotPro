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

/** Free sign-up (no payment) — Spark plan / "Start free" CTAs. */
export const SIGNUP_HREF = "/login?next=/garage";
/** Self-serve paid checkout (Builder / Fleet) — gated by sign-in inside /upgrade. */
export const UPGRADE_HREF = "/upgrade";

export const DOCS_HREF = "https://docs.ohho.ai";
export const GITHUB_HREF = "https://github.com/anomalyco/OmniBotPro";
export const TWITTER_HREF = "https://x.com/ohho_ai";
export const LINKEDIN_HREF = "https://linkedin.com/company/ohho-ai";

/** mailto: link with a pre-filled subject (used by Get Started / plan CTAs). */
export function contactMailto(subject: string): string {
  return `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}`;
}

/** Detail-page route for a product slug. */
export function productHref(slug: string): string {
  return `/products/${slug}`;
}
