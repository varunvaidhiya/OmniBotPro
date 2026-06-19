import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import AutonomyConsole from "@/components/autonomy/AutonomyConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Autonomy — Map it, navigate it, command it in plain language.",
  description:
    "SLAM mapping, Nav2 navigation and a mission planner driven by a natural-language agent that turns an instruction into a sequence of robot actions.",
};

export default function AutonomyPage() {
  return (
    <ConsoleGate product="autonomy">
      <RobotProvider>
        <AutonomyConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
