import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import ShieldConsole from "@/components/shield/ShieldConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Shield — Security for robots.",
  description:
    "Give every robot a hardware identity, encrypt its links, sign its updates, and watch its software bill of materials.",
  robots: NO_INDEX,
};

export default function ShieldPage() {
  return (
    <ConsoleGate product="shield">
      <RobotProvider>
        <ShieldConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
