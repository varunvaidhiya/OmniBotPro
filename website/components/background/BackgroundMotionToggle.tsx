"use client";

import { Pause, Play } from "lucide-react";

import { MOTION_PREF_KEY } from "@/lib/background";

/*
 * BackgroundMotionToggle — the visitor's escape hatch from the background reel.
 *
 * A moving backdrop is a preference, not a fact, so anyone can stop it in one
 * click and the choice is remembered. It sits bottom-left (the assistant
 * launcher owns bottom-right), rests at low opacity so it never competes with
 * the page, and comes fully forward on hover or keyboard focus.
 */
export default function BackgroundMotionToggle({
  on,
  onChange,
}: {
  on: boolean;
  onChange: (next: boolean) => void;
}) {
  const toggle = () => {
    const next = !on;
    try {
      window.localStorage.setItem(MOTION_PREF_KEY, next ? "on" : "off");
    } catch {
      // storage blocked — the choice just won't survive a reload
    }
    // SiteBackground listens for this so the change applies without a reload.
    window.dispatchEvent(new Event("ohho:bg-motion-change"));
    onChange(next);
  };

  return (
    <button
      type="button"
      onClick={toggle}
      className="bg-motion-toggle"
      aria-pressed={on}
      title={on ? "Pause the background video" : "Play the background video"}
    >
      {on ? <Pause size={11} strokeWidth={2.2} /> : <Play size={11} strokeWidth={2.2} />}
      <span>{on ? "Pause background" : "Play background"}</span>
    </button>
  );
}
