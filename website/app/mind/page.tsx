import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import MindConsole from "@/components/mind/MindConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Mind — Give your robot a mind of its own.",
  description:
    "The deliberative agent brain: a continuous perceive → reason → verify → act → monitor → reflect → remember loop with a hardware-safety gate, hybrid reasoning and memory.",
  robots: NO_INDEX,
};

export default function MindPage() {
  return (
    <ConsoleGate product="mind">
      <RobotProvider>
        <MindConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
