import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import FleetConsole from "@/components/fleet/FleetConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Fleet — Mission Control.",
  description:
    "Update 50 robots like you update an app. Fleet management, signed over-the-air updates and a full observability stack.",
};

export default function FleetPage() {
  return (
    <ConsoleGate product="fleet">
      <RobotProvider>
        <FleetConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
