"use client";

import dynamic from "next/dynamic";

// react-three-fiber is browser-only (the site is statically exported), so the
// scene is loaded with ssr disabled.
const RobotScene = dynamic(() => import("./RobotScene"), {
  ssr: false,
  loading: () => null,
});

/*
 * RobotBackground — a fixed, full-viewport 3-D layer behind every section. It
 * is pointer-events:none so it never blocks scrolling or clicks; the robot
 * still follows the cursor because OmniBotModel tracks the pointer globally.
 *
 * The layer is position:fixed, so it stays pinned to the viewport while the
 * page content scrolls over it — the robot itself does not move on scroll.
 * (No scroll parallax: an independent scroll-velocity offset on top of the
 * normal page scroll read as a "double scroll" / jerk.) robotParallax.offsetPx
 * therefore stays 0, which keeps OmniBotModel's cursor → floor projection
 * accurate.
 */
export default function RobotBackground() {
  return (
    <div className="robot-bg" aria-hidden="true">
      <div className="robot-bg-inner">
        <RobotScene />
      </div>
    </div>
  );
}
