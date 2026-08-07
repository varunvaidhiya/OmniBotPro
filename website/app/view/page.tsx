import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import ViewConsole from "@/components/view/ViewConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO View — Remote perception console.",
  description:
    "Visualize 3D point clouds, SLAM maps, camera streams, and telemetry in a real-time web console.",
  robots: NO_INDEX,
};

export default function ViewPage() {
  return (
    <ConsoleGate product="view">
      <RobotProvider>
        <ViewConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
