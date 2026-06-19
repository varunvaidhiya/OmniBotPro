/*
 * OhhO Build — client helper for natural-language design generation.
 *
 * Talks to the server-side route /api/build/generate (which holds the Kimi /
 * Moonshot API key) and returns a sanitized Design ready to load into the
 * studio via dispatch({ type: "loadDesign", design }).
 */

import { sanitize, type Design, type Requirements } from "./design";

export async function generateDesign(
  prompt: string,
  requirements?: Partial<Requirements>,
  signal?: AbortSignal,
): Promise<Design> {
  const res = await fetch("/api/build/generate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ prompt, requirements }),
    signal,
  });

  const data = (await res.json().catch(() => null)) as { design?: Design; error?: string } | null;

  if (!res.ok) {
    const msg = (data && typeof data.error === "string" && data.error) || `Request failed (${res.status}).`;
    throw new Error(msg);
  }
  if (!data || typeof data.design !== "object") {
    throw new Error("The server did not return a valid design.");
  }
  // sanitize again on the client as defense in depth.
  return sanitize(data.design as Design);
}
