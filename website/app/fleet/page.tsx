import type { Metadata } from "next";
import FleetConsole from "@/components/fleet/FleetConsole";

export const metadata: Metadata = {
  title: "OhhO Fleet — Mission Control.",
  description:
    "Update 50 robots like you update an app. Fleet management, signed over-the-air updates and a full observability stack.",
};

export default function FleetPage() {
  return <FleetConsole />;
}
