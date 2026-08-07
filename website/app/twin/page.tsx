import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import TwinConsole from "@/components/twin/TwinConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Twin — Your real robot. Mirrored in simulation. Live.",
  description:
    "A live digital twin that streams real robot telemetry into a persistent simulation — replay, scrub, what-if and predict, side by side with the physical robot.",
  robots: NO_INDEX,
};

export default function TwinPage() {
  return (
    <ConsoleGate product="twin">
      <RobotProvider>
        <TwinConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
