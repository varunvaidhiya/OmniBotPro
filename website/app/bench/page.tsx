import type { Metadata } from "next";
import BenchConsole from "@/components/bench/BenchConsole";

export const metadata: Metadata = {
  title: "OhhO Bench — From a box of parts to a robot that powers on.",
  description:
    "Guided assembly, wiring and firmware bring-up: turn a Build BOM into a wired, flashed and calibrated robot with hardware self-tests.",
};

export default function BenchPage() {
  return <BenchConsole />;
}
