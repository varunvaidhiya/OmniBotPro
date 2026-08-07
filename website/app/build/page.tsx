import type { Metadata } from "next";
import BuildStudio from "@/components/build/BuildStudio";
import ConsoleGate from "@/components/auth/ConsoleGate";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Build — Design any robot, in your browser",
  description:
    "Drag real components onto a 3-D canvas, set your requirements, and get an AI-validated design with a sourced bill of materials — then export a URDF, sim world and deployment profile to the OhhO stack.",
  robots: NO_INDEX,
};

// The studio is a fully client-side app (3-D canvas, localStorage, shareable
// ?d= URLs) — nothing to pre-render but its shell. Gated behind an active plan.
export default function BuildPage() {
  return (
    <ConsoleGate product="build">
      <RobotProvider>
        <BuildStudio />
      </RobotProvider>
    </ConsoleGate>
  );
}
