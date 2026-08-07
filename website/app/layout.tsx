import type { Metadata, Viewport } from "next";
import "./globals.css";
import AuthRedirectHandler from "@/components/auth/AuthRedirectHandler";
import SiteBackground from "@/components/background/SiteBackground";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import { RobotConnectionProvider } from "@/lib/connect/RobotConnectionProvider";
import ConnectionBar from "@/components/connect/ConnectionBar";
import AssistantMount from "@/components/assistant/AssistantMount";
import FirebaseAnalytics from "@/components/analytics/FirebaseAnalytics";

import {
  SITE_URL,
  SITE_NAME,
  SITE_SHORT_NAME,
  DEFAULT_DESCRIPTION,
  OG_IMAGE,
} from "@/lib/seo";
import { GITHUB_HREF, TWITTER_HREF, LINKEDIN_HREF } from "@/lib/site";

const HOME_TITLE = "OhhO Robotics — Open-Source Robots, First. Open Ecosystems, Welcome.";

// Without the viewport export, mobile browsers default to a 980px virtual
// viewport and Tailwind's `md:flex` breakpoint fires unreliably — that was
// the root cause of "sometimes the nav bar is visible, sometimes not."
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 5,
  themeColor: "#0a0e1a",
};

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  // Child pages supply a short title; the template appends the brand so every
  // tab and every search result carries the name people actually search for.
  title: { default: HOME_TITLE, template: `%s | ${SITE_NAME}` },
  description: DEFAULT_DESCRIPTION,
  applicationName: SITE_NAME,
  // Homepage canonical. Every other route overrides this via lib/seo#pageSeo —
  // if you add a page, add its canonical too or it inherits this one.
  alternates: { canonical: SITE_URL },
  icons: {
    icon: "/icon.svg",
    shortcut: "/icon.svg",
    apple: "/icon.svg",
  },
  openGraph: {
    title: HOME_TITLE,
    description: DEFAULT_DESCRIPTION,
    url: SITE_URL,
    siteName: SITE_NAME,
    images: [{ url: OG_IMAGE, width: 1200, height: 630, alt: SITE_NAME }],
    locale: "en_US",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: HOME_TITLE,
    description: DEFAULT_DESCRIPTION,
    images: [OG_IMAGE],
  },
};

/*
 * Structured data. `alternateName` is the part that matters for the brand
 * query: the company is written "OhhO" in the product copy but people search
 * "OhhO Robotics" (the domain), so both spellings are declared as names of the
 * same entity. `sameAs` corroborates that entity against its public profiles.
 */
const SOCIAL_PROFILES = [GITHUB_HREF, TWITTER_HREF, LINKEDIN_HREF];

const orgJsonLd = {
  "@context": "https://schema.org",
  "@type": "Organization",
  "@id": `${SITE_URL}/#organization`,
  name: SITE_NAME,
  alternateName: [SITE_SHORT_NAME, "OhhO AI", "OhhO Robotics Platform"],
  url: SITE_URL,
  logo: {
    "@type": "ImageObject",
    url: `${SITE_URL}/icon.svg`,
    caption: SITE_NAME,
  },
  description: DEFAULT_DESCRIPTION,
  founder: { "@type": "Person", name: "Varun Vaidhiya" },
  sameAs: SOCIAL_PROFILES,
};

const siteJsonLd = {
  "@context": "https://schema.org",
  "@type": "WebSite",
  "@id": `${SITE_URL}/#website`,
  name: SITE_NAME,
  alternateName: [SITE_SHORT_NAME, "OhhO Robotics Platform"],
  url: SITE_URL,
  description: DEFAULT_DESCRIPTION,
  inLanguage: "en-US",
  publisher: { "@id": `${SITE_URL}/#organization` },
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
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(siteJsonLd) }}
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
