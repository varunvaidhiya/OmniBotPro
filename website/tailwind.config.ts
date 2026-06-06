import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        bg: "#0A0E1A",
        surf: "#0F1628",
        "surf-hi": "#141D35",
        cyan: "#00D4FF",
        violet: "#7C3AED",
        "violet-lite": "#A78BFA",
      },
      fontFamily: {
        display: ["Space Grotesk", "sans-serif"],
        body: ["Inter", "sans-serif"],
        mono: ["JetBrains Mono", "monospace"],
      },
      maxWidth: {
        content: "1160px",
      },
      animation: {
        "grid-drift": "grid-drift 28s linear infinite",
        "orb1": "orb1 18s ease-in-out infinite alternate",
        "orb2": "orb2 14s ease-in-out infinite alternate",
        "bdot": "bdot 2.2s ease-in-out infinite",
        "scroll-hint": "scroll-hint 2s ease-in-out infinite",
      },
      keyframes: {
        "grid-drift": {
          from: { backgroundPosition: "0 0" },
          to: { backgroundPosition: "68px 68px" },
        },
        orb1: {
          from: { transform: "translate(0,0)" },
          to: { transform: "translate(70px,-50px)" },
        },
        orb2: {
          from: { transform: "translate(0,0)" },
          to: { transform: "translate(-60px,40px)" },
        },
        bdot: {
          "0%,100%": { opacity: "1", transform: "scale(1)" },
          "50%": { opacity: "0.4", transform: "scale(0.65)" },
        },
        "scroll-hint": {
          "0%,100%": { transform: "translateX(-50%) translateY(0)", opacity: "0.5" },
          "50%": { transform: "translateX(-50%) translateY(7px)", opacity: "1" },
        },
      },
    },
  },
  plugins: [],
};

export default config;
