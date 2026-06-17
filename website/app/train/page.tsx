import type { Metadata } from "next";
import TrainConsole from "@/components/train/TrainConsole";

export const metadata: Metadata = {
  title: "OhhO Train — Turn demonstrations into policies.",
  description:
    "Fine-tune VLA, imitation and reinforcement-learning policies with live loss/success curves, then export straight to Serve and Fleet.",
};

export default function TrainPage() {
  return <TrainConsole />;
}
