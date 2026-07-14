"use client";

/*
 * Minimal client-side A/B experiment hook.
 *
 * Phase 0 scaffolding for testing hero messaging. A visitor is bucketed once
 * (stable in localStorage) and their exposure is reported through analytics, so
 * we can compare CTA conversion per variant later. SSR-safe: the server and the
 * first client render both return variants[0] (no hydration mismatch); the real
 * bucket is applied in an effect after mount.
 */

import { useEffect, useState } from "react";
import { track } from "@/lib/analytics";

export interface Experiment<V extends string> {
  key: string;
  variants: readonly V[];
}

/** Return this visitor's stable variant for an experiment, reporting exposure once. */
export function useExperiment<V extends string>(exp: Experiment<V>): V {
  const [variant, setVariant] = useState<V>(exp.variants[0]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const storageKey = `ohho_exp_${exp.key}`;
    let chosen: V | null = null;
    try {
      const saved = window.localStorage.getItem(storageKey) as V | null;
      if (saved && exp.variants.includes(saved)) chosen = saved;
    } catch {
      /* localStorage may be unavailable (private mode) — fall through */
    }
    if (!chosen) {
      chosen = exp.variants[Math.floor(Math.random() * exp.variants.length)];
      try {
        window.localStorage.setItem(storageKey, chosen);
      } catch {
        /* ignore */
      }
    }
    setVariant(chosen);
    track("experiment_exposure", { experiment: exp.key, variant: chosen });
  }, [exp]);

  return variant;
}

/** The hero-headline experiment: two wedge-aligned framings. */
export const HERO_HEADLINE_EXPERIMENT = {
  key: "hero_headline",
  variants: ["data_gravity", "affordable"] as const,
};
