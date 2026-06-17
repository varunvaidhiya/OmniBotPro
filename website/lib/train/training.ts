/*
 * Simulated training-run state for OhhO Train.
 *
 * Drives the live loss / success-rate curves and GPU telemetry while a run is
 * "training", and gates the export-to-Serve step on a passing eval. All
 * client-side simulation — no GPU is harmed.
 */

import { useCallback, useEffect, useRef, useState } from "react";

export type RunState = "idle" | "running" | "paused" | "done";

export interface TrainConfig {
  method: string;
  dataset: string;
  epochs: number;
  lr: string;
  device: string;
}

export const METHODS = ["smolvla", "act", "diffusion", "openvla", "rl (ppo)"];

export interface Gpu {
  util: number;
  vram: number; // GB used
  temp: number; // °C
}

const TOTAL_STEPS = 50_000;
const MAX_POINTS = 40;

export function useTraining() {
  const [config, setConfig] = useState<TrainConfig>({
    method: "smolvla",
    dataset: "mobile_manipulation · 1,043 eps",
    epochs: 50,
    lr: "1e-4",
    device: "cuda:0",
  });

  const [state, setState] = useState<RunState>("idle");
  const [step, setStep] = useState(0);
  const [loss, setLoss] = useState<number[]>([0.95]);
  const [success, setSuccess] = useState<number[]>([0.12]);
  const [gpu, setGpu] = useState<Gpu>({ util: 0, vram: 1.2, temp: 38 });
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);

  const stop = useCallback(() => {
    if (timer.current) clearInterval(timer.current);
    timer.current = null;
  }, []);

  useEffect(() => stop, [stop]);

  const tick = useCallback(() => {
    setStep((s) => {
      const next = Math.min(TOTAL_STEPS, s + 900 + Math.random() * 300);
      if (next >= TOTAL_STEPS) {
        stop();
        setState("done");
        setGpu({ util: 0, vram: 1.2, temp: 40 });
      }
      const frac = next / TOTAL_STEPS;
      // loss decays, success climbs — both with a little noise
      setLoss((prev) => {
        const v = Math.max(0.04, 0.95 * Math.exp(-3.1 * frac) + (Math.random() - 0.5) * 0.03);
        const arr = [...prev, v];
        return arr.length > MAX_POINTS ? arr.slice(arr.length - MAX_POINTS) : arr;
      });
      setSuccess((prev) => {
        const v = Math.min(0.97, 0.12 + 0.85 * (1 - Math.exp(-2.7 * frac)) + (Math.random() - 0.5) * 0.02);
        const arr = [...prev, v];
        return arr.length > MAX_POINTS ? arr.slice(arr.length - MAX_POINTS) : arr;
      });
      setGpu({
        util: 70 + Math.random() * 25,
        vram: 13.5 + Math.random() * 1.2,
        temp: 62 + Math.random() * 6,
      });
      return next;
    });
  }, [stop]);

  const start = useCallback(() => {
    if (state === "done") {
      // restart
      setStep(0);
      setLoss([0.95]);
      setSuccess([0.12]);
    }
    setState("running");
    stop();
    timer.current = setInterval(tick, 700);
  }, [state, stop, tick]);

  const pause = useCallback(() => {
    stop();
    setState("paused");
    setGpu((g) => ({ ...g, util: 0 }));
  }, [stop]);

  const reset = useCallback(() => {
    stop();
    setState("idle");
    setStep(0);
    setLoss([0.95]);
    setSuccess([0.12]);
    setGpu({ util: 0, vram: 1.2, temp: 38 });
  }, [stop]);

  const successNow = success[success.length - 1];
  const lossNow = loss[loss.length - 1];
  const epoch = Math.round((step / TOTAL_STEPS) * config.epochs);
  const canExport = successNow >= 0.8 && (state === "running" || state === "done");

  return {
    config,
    setConfig,
    state,
    step,
    totalSteps: TOTAL_STEPS,
    epoch,
    loss,
    success,
    lossNow,
    successNow,
    gpu,
    canExport,
    start,
    pause,
    reset,
  };
}
