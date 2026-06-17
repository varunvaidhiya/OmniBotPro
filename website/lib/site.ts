/*
 * Central place for the site's "where does this button go" links, so the
 * purchase / get-started path is defined once and easy to change.
 *
 * There is no checkout backend yet, so paid actions open a pre-filled email.
 * Swap CONTACT_EMAIL (or repoint these helpers at a real signup flow) when the
 * billing flow exists.
 */

export const CONTACT_EMAIL = "hello@ohho.ai";

export const PRICING_HREF = "/#pricing";
export const PRODUCTS_HREF = "/#products";

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
