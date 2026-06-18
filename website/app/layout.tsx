import type { Metadata } from "next";
import "./globals.css";
import RobotBackground from "@/components/robot/RobotBackground";
import { AuthProvider } from "@/lib/auth/AuthProvider";

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
          {/* live 3-D OmniBot — fixed behind every section, follows the cursor */}
          <RobotBackground />
          <div className="content-layer">{children}</div>
        </AuthProvider>
      </body>
    </html>
  );
}
