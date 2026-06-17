import type { Metadata } from "next";
import FrameConsole from "@/components/frame/FrameConsole";

export const metadata: Metadata = {
  title: "OhhO Frame — Edge computation OS.",
  description:
    "A lean, real-time Linux container optimized for high-frequency control loops and neural network inference.",
};

export default function FramePage() {
  return <FrameConsole />;
}
