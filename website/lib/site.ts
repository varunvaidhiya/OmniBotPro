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

/** mailto: link with a pre-filled subject (used by Get Started / plan CTAs). */
export function contactMailto(subject: string): string {
  return `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}`;
}

/** Detail-page route for a product slug. */
export function productHref(slug: string): string {
  return `/products/${slug}`;
}
