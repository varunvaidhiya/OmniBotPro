import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import CareConsole from "@/components/care/CareConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Care — Fix it before it breaks.",
  description:
    "Predictive maintenance and service workflow for robot fleets. Turn motor-degradation signals from OhhO Fleet into scheduled service, ordered parts and logged repairs.",
};

export default function CarePage() {
  return (
    <ConsoleGate product="care">
      <RobotProvider>
        <CareConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
