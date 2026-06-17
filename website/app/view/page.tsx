import type { Metadata } from "next";
import ViewConsole from "@/components/view/ViewConsole";

export const metadata: Metadata = {
  title: "OhhO View — Remote perception console.",
  description:
    "Visualize 3D point clouds, SLAM maps, camera streams, and telemetry in a real-time web console.",
};

export default function ViewPage() {
  return <ViewConsole />;
}
