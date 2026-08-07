import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import BenchConsole from "@/components/bench/BenchConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Bench — From a box of parts to a robot that powers on.",
  description:
    "Guided assembly, wiring and firmware bring-up: turn a Build BOM into a wired, flashed and calibrated robot with hardware self-tests.",
  robots: NO_INDEX,
};

export default function BenchPage() {
  return (
    <ConsoleGate product="bench">
      <RobotProvider>
        <BenchConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
