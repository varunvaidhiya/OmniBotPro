import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import PilotConsole from "@/components/pilot/PilotConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Pilot — Operate any robot, from anywhere",
  description:
    "VR and mobile teleoperation in real time. Pick a robot profile, drive with a virtual joystick, command an arm, and see what the robot sees — a working cockpit for the OhhO Pilot teleop system.",
  robots: NO_INDEX,
};

// Fully client-side cockpit (simulated camera, joystick, arm sliders, telemetry)
// — nothing to pre-render but its shell.
export default function PilotPage() {
  return (
    <ConsoleGate product="pilot">
      <RobotProvider>
        <PilotConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
