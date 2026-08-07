import { ImageResponse } from "next/og";

/*
 * The social/search preview card, rendered to a real PNG at build time.
 *
 * The previous og:image pointed at /ohho-logo.svg. Google, X, LinkedIn and
 * Slack all ignore SVG for preview images, so every share of this site was
 * rendering with no image at all.
 */

export const runtime = "nodejs";
export const alt = "OhhO Robotics — the open-source robotics platform";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OpengraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          background: "#05070d",
          padding: "72px 80px",
          fontFamily: "sans-serif",
        }}
      >
        {/* accent rule */}
        <div style={{ display: "flex", width: 180, height: 8, background: "#22d3ee" }} />

        <div style={{ display: "flex", flexDirection: "column" }}>
          <div
            style={{
              fontSize: 82,
              fontWeight: 700,
              color: "#ffffff",
              letterSpacing: "-0.03em",
              lineHeight: 1.05,
            }}
          >
            OhhO Robotics
          </div>
          <div
            style={{
              fontSize: 38,
              color: "#22d3ee",
              marginTop: 20,
              letterSpacing: "-0.01em",
            }}
          >
            Open-source robots, first.
          </div>
          <div
            style={{
              fontSize: 27,
              color: "rgba(255,255,255,0.62)",
              marginTop: 26,
              lineHeight: 1.4,
              maxWidth: 900,
            }}
          >
            Build, train, simulate, deploy, manage and regulate any robot. No vendor lock-in, ever.
          </div>
        </div>

        <div style={{ display: "flex", fontSize: 25, color: "rgba(255,255,255,0.45)" }}>
          ohho-robotics.com
        </div>
      </div>
    ),
    size,
  );
}
