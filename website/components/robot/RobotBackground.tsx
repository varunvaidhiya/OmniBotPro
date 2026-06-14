"use client";

import dynamic from "next/dynamic";
import { useEffect, useRef } from "react";
import { robotParallax } from "./robotParallax";

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
 * Scroll parallax (smooth + continuous):
 *   The inner canvas is lifted by an *inertial*, velocity-driven offset. A
 *   continuous rAF loop reads scroll velocity every frame, so the robot glides
 *   up (and beyond the top of the frame on a fast scroll) while you scroll, and
 *   always eases back to a centred, fully-visible resting position the moment
 *   you stop — it is never parked half-off-frame between sections. Double
 *   smoothing (velocity low-pass + offset easing) keeps the motion fluid.
 *   The live offset is shared via robotParallax so the cursor mapping stays
 *   accurate while the layer is shifted.
 */
export default function RobotBackground() {
  const innerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    let lastScrollY = window.scrollY;
    let vel = 0; // low-passed scroll velocity (px/frame)
    let current = 0; // applied vertical offset (px)
    let raf = 0;
    let running = true;

    const loop = () => {
      if (!running) return;
      const h = window.innerHeight || 1;
      const y = window.scrollY;

      // smooth the raw per-frame scroll delta into a velocity
      vel += (y - lastScrollY - vel) * 0.25;
      lastScrollY = y;

      // velocity → target lift: scrolling down (vel > 0) raises the robot.
      // Clamped so a vigorous scroll can carry it beyond the top edge, while a
      // gentle scroll only nudges it. At rest vel → 0, so target → 0 and the
      // robot recentres (always fully visible when you stop).
      const maxUp = 0.8 * h;
      const maxDown = 0.22 * h;
      let target = -vel * 8;
      if (target < -maxUp) target = -maxUp;
      if (target > maxDown) target = maxDown;

      current += (target - current) * 0.12; // eased follow → fluid, no jumps
      if (Math.abs(current) < 0.05 && Math.abs(vel) < 0.05) current = 0;

      robotParallax.offsetPx = current;
      if (innerRef.current) {
        innerRef.current.style.transform = `translate3d(0, ${current.toFixed(2)}px, 0)`;
      }
      raf = requestAnimationFrame(loop);
    };

    raf = requestAnimationFrame(loop);
    return () => {
      running = false;
      cancelAnimationFrame(raf);
      robotParallax.offsetPx = 0;
    };
  }, []);

  return (
    <div className="robot-bg" aria-hidden="true">
      <div ref={innerRef} className="robot-bg-inner">
        <RobotScene />
      </div>
    </div>
  );
}
