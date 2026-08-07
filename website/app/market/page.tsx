import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import MarketConsole from "@/components/market/MarketConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Market — Download a skill. Or sell one.",
  description:
    "A cross-brand marketplace for trained robot skills and policies. Download a verified pick-and-place policy for your G1, or publish one you trained with OhhO Train.",
  robots: NO_INDEX,
};

export default function MarketPage() {
  return (
    <ConsoleGate product="market">
      <RobotProvider>
        <MarketConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
