import type { Metadata } from "next";
import DataConsole from "@/components/data/DataConsole";

export const metadata: Metadata = {
  title: "OhhO Data — Collect. Label. Ship.",
  description:
    "An end-to-end pipeline for robot demonstration data. Teleop recording, an episode viewer and CLI tools — in LeRobot-compatible format, training-ready.",
};

// Fully client-side data viewer (simulated episodes, scrubbing, plots)
export default function DataPage() {
  return <DataConsole />;
}
