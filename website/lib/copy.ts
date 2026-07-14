/*
 * Marketing copy deck — the single source of truth for the landing page's words.
 *
 * Phase 0 of the repositioning: the hero leads with the WEDGE (open data +
 * deployment for affordable robots) and states data-gravity + open standards as
 * the advantage. "No vendor lock-in" is demoted from the moat to a trust bullet
 * (it lowers the barrier to *adopt*, so it belongs in the trust row — it is not,
 * by itself, defensibility). Hero / StatsBar / Footer all import from here so the
 * message stays consistent across the site.
 */

export const WORDMARK = "OhhO";
/** Trademark notice for the footer (the brand is defensible even when the code is Apache). */
export const TRADEMARK_LINE = "OhhO™ is a trademark of OhhO Robotics.";
export const FOOTER_TAGLINE = "Robotics, Operated.";

/** Small badge above the wordmark. */
export const HERO_BADGE = "Open data + deployment · ROS 2 · LeRobot-native";

/**
 * Trust row under the value prop. This is where portability / "no lock-in" now
 * lives — as a reason it is safe to try us, not as the headline advantage.
 */
export const HERO_TRUST: string[] = [
  "Apache-2.0 engine",
  "Works with LeRobot, ROS 2 & ONNX",
  "Runs on a Pi, a laptop, or your cloud",
  "Your data, portable forever",
];

/** Hero A/B variants (both lead with the wedge; we test framings, not new-vs-old). */
export type HeroVariant = "data_gravity" | "affordable";

export const HERO_COPY: Record<HeroVariant, { subtag: string; valueProp: string }> = {
  data_gravity: {
    subtag: "The open data & deployment layer for robots",
    valueProp:
      "Collect, train, and run policies on any affordable robot — and your data compounds with you. Open standards, no lock-in, runs anywhere.",
  },
  affordable: {
    subtag: "The open stack for affordable robots",
    valueProp:
      "Give any low-cost robot a brain: record demonstrations, train a policy, deploy it. Built on open standards, runs on a Pi, a laptop, or your cloud.",
  },
};

/** The one-liner used in meta/OG and internal reference. */
export const WEDGE_ONELINER =
  "The open data-and-deployment layer for affordable robots — where openness gets you in and your data keeps you.";

/** StatsBar tiles — honest, aligned to the positioning (no "19 consoles" over-claim). */
export const STATS: { n: string; label: string }[] = [
  { n: "Any", label: "robot — LeRobot · ROS 2 · ONNX compatible" },
  { n: "Open", label: "Apache-2.0 engine — self-host or OhhO Cloud" },
  { n: "1 link", label: "connect any robot — Wi-Fi, USB, BLE or sim" },
  { n: "$0", label: "platform fee — pay only for the cloud you use" },
];

/** Products section intro — honest "core now, rest on the roadmap" framing. */
export const PRODUCTS_INTRO = {
  eyebrow: "Platform",
  headingTop: "Start with the core.",
  headingBottom: "Grow into the platform.",
  body:
    "OhhO's core — Connect, Data, Train and Serve — is available now: connect any robot, collect demonstrations, train a policy, and deploy it. The rest of the platform ships console by console. All open, all portable, all on one engine — OhhO OS.",
};
