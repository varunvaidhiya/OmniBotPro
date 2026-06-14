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
 * RobotBackground — a fixed, full-viewport 3-D layer that lives behind every
 * page section. It is pointer-events:none so it never blocks scrolling or
 * clicks; the robot still follows the cursor because OmniBotModel tracks the
 * pointer globally off the window.
 *
 * Scroll parallax: the inner canvas is translated vertically as you scroll, so
 * the robot rises up and beyond the top of the frame, then eases back into view
 * on a gentle cycle (offset = -A·(1 − cos θ)/2, θ ∝ scrollY). The viewport
 * edges are feathered in CSS so the robot dissolves cleanly as it leaves frame.
 * The live offset is shared via robotParallax so the cursor mapping stays true.
 */
export default function RobotBackground() {
  const innerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    let target = 0;
    let current = 0;
    let raf = 0;

    const apply = () => {
      robotParallax.offsetPx = current;
      if (innerRef.current) {
        innerRef.current.style.transform = `translate3d(0, ${current.toFixed(2)}px, 0)`;
      }
    };

    const compute = () => {
      const h = window.innerHeight || 1;
      const amplitude = 0.6 * h; // how far the robot travels up out of frame
      const period = 1.4 * h; // scroll distance for one full up-and-back cycle
      const theta = (window.scrollY / period) * Math.PI * 2;
      target = -amplitude * (1 - Math.cos(theta)) * 0.5; // 0 → -A → 0, scroll-linked
    };

    const tick = () => {
      current += (target - current) * 0.12; // eased follow, like the cursor chase
      if (Math.abs(target - current) < 0.1) {
        current = target;
        apply();
        raf = 0;
        return;
      }
      apply();
      raf = requestAnimationFrame(tick);
    };

    const onScroll = () => {
      compute();
      if (!raf) raf = requestAnimationFrame(tick);
    };

    compute();
    apply();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });
    return () => {
      if (raf) cancelAnimationFrame(raf);
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
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
