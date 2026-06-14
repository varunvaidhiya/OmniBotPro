"use client";

import dynamic from "next/dynamic";

// react-three-fiber is browser-only (the site is statically exported), so the
// scene is loaded with ssr disabled.
const RobotScene = dynamic(() => import("./RobotScene"), {
  ssr: false,
  loading: () => null,
});

/*
 * RobotBackground — a fixed, full-viewport 3-D layer that lives behind every
 * page section. It is pointer-events:none so it never blocks scrolling or
 * clicks; the robot still follows the cursor because OmniBotModel tracks the
 * pointer globally off the window.
 */
export default function RobotBackground() {
  return (
    <div className="robot-bg" aria-hidden="true">
      <RobotScene />
    </div>
  );
}
