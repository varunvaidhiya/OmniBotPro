import type { Metadata } from "next";
import "./globals.css";
import RobotBackground from "@/components/robot/RobotBackground";
import AuthRedirectHandler from "@/components/auth/AuthRedirectHandler";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import { RobotConnectionProvider } from "@/lib/connect/RobotConnectionProvider";
import ConnectionBar from "@/components/connect/ConnectionBar";
import AssistantMount from "@/components/assistant/AssistantMount";

export const metadata: Metadata = {
  title: "OhhO — Robotics, Operated.",
  description:
    "From VR teleoperation to AI inference — one platform to build, deploy and scale any robot.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
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
