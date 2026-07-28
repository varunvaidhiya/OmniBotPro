import type { Metadata } from "next";
import "./globals.css";
import AuthRedirectHandler from "@/components/auth/AuthRedirectHandler";
import SiteBackground from "@/components/background/SiteBackground";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import { RobotConnectionProvider } from "@/lib/connect/RobotConnectionProvider";
import ConnectionBar from "@/components/connect/ConnectionBar";
import AssistantMount from "@/components/assistant/AssistantMount";
import FirebaseAnalytics from "@/components/analytics/FirebaseAnalytics";

const SITE_URL = "https://ohho-robotics.com";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  title: "OhhO — Open-Source Robots, First. Open Ecosystems, Welcome.",
  description:
    "The open-source robotics platform. Built for open-source robots and hardware first — and works with any brand that keeps its software layer open and welcomes developers. Build, train, launch, deploy, manage and regulate any robot. No vendor lock-in, ever.",
  icons: {
    icon: "/icon.svg",
    shortcut: "/icon.svg",
    apple: "/icon.svg",
  },
  openGraph: {
    title: "OhhO — Open-Source Robots, First. Open Ecosystems, Welcome.",
    description:
      "The open-source robotics platform. Built for open-source robots and hardware first — and works with any brand that keeps its software layer open and welcomes developers. No vendor lock-in, ever.",
    url: SITE_URL,
    siteName: "OhhO",
    images: [{ url: "/ohho-logo.svg", width: 1200, height: 630, alt: "OhhO — Open-Source Robots, First." }],
    locale: "en_US",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: "OhhO — Open-Source Robots, First. Open Ecosystems, Welcome.",
    description:
      "The open-source robotics platform. Built for open-source robots and hardware first — and works with any brand that keeps its software layer open and welcomes developers. No vendor lock-in, ever.",
    images: ["/ohho-logo.svg"],
  },
};

const orgJsonLd = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "OhhO",
  url: SITE_URL,
  logo: `${SITE_URL}/icon.svg`,
  description:
    "The open-source robotics platform. Built for open-source robots and hardware first — and works with any brand that keeps its software layer open and welcomes developers. No vendor lock-in, ever.",
  founder: { "@type": "Person", name: "Varun Vaidhiya" },
  sameAs: ["https://github.com/varunvaidhiya/OmniBotPro"],
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(orgJsonLd) }}
        />
        {/* Privacy-friendly analytics — inert until NEXT_PUBLIC_PLAUSIBLE_DOMAIN is
            set. Once set, it powers the funnel + hero A/B events emitted through
            lib/analytics.ts (window.plausible). Override the src with
            NEXT_PUBLIC_PLAUSIBLE_SRC to point at a self-hosted instance. */}
        {process.env.NEXT_PUBLIC_PLAUSIBLE_DOMAIN && (
          <script
            defer
            data-domain={process.env.NEXT_PUBLIC_PLAUSIBLE_DOMAIN}
            src={process.env.NEXT_PUBLIC_PLAUSIBLE_SRC || "https://plausible.io/js/script.js"}
          />
        )}
      </head>
      <body>
        {/* Firebase Analytics (GA4) — inert until NEXT_PUBLIC_FIREBASE_* is set.
            Warms the SDK and emits a page_view on every route, including
            client-side navigations. See FIREBASE_ANALYTICS_SETUP.md. */}
        <FirebaseAnalytics />
        {/* fixed cinematic reel behind every page — sits below .content-layer */}
        <SiteBackground />
        <AuthProvider>
          <RobotConnectionProvider>
            <AuthRedirectHandler />
            {/* global "connected robot" HUD — shows on every operational console */}
            <ConnectionBar />
            <div className="content-layer">{children}</div>
            {/* in-console AI assistant — same launcher on every console */}
            <AssistantMount />
          </RobotConnectionProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
