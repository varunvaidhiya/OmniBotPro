import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import DataConsole from "@/components/data/DataConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Data — Collect. Label. Ship.",
  description:
    "An end-to-end pipeline for robot demonstration data. Teleop recording, an episode viewer and CLI tools — in LeRobot-compatible format, training-ready.",
  robots: NO_INDEX,
};

// Fully client-side data viewer (simulated episodes, scrubbing, plots)
export default function DataPage() {
  return (
    <ConsoleGate product="data">
      <RobotProvider>
        <DataConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
