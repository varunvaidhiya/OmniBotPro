import type { Metadata } from "next";
import "./globals.css";
import RobotBackground from "@/components/robot/RobotBackground";
import AuthRedirectHandler from "@/components/auth/AuthRedirectHandler";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import { RobotConnectionProvider } from "@/lib/connect/RobotConnectionProvider";
import ConnectionBar from "@/components/connect/ConnectionBar";
import AssistantMount from "@/components/assistant/AssistantMount";

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
      </head>
      <body>
        <AuthProvider>
          <RobotConnectionProvider>
            <AuthRedirectHandler />
            {/* live 3-D OmniBot — fixed behind every section, follows the cursor */}
            <RobotBackground />
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
