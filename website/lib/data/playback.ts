/*
 * Playback hook and state for the OhhO Data episode viewer.
 *
 * Uses requestAnimationFrame to drive timeline playback at ~30 FPS,
 * allowing scrubbing and play/pause controls.
 */

import { useEffect, useRef, useState, useCallback } from "react";
import type { Episode } from "./episodes";

export interface PlaybackState {
  playing: boolean;
  frame: number;
  /** Normalized progress [0, 1] */
  progress: number;
}

export function usePlayback(episode: Episode) {
  const [playing, setPlaying] = useState(false);
  const [frame, setFrame] = useState(0);
  const rafRef = useRef<number>(0);

  // Reset when episode changes
  useEffect(() => {
    setPlaying(false);
    setFrame(0);
  }, [episode.id]);

  const togglePlayback = useCallback(() => {
    setPlaying((p) => !p);
  }, []);

  const seek = useCallback((newFrame: number) => {
    setFrame(Math.max(0, Math.min(episode.frames - 1, newFrame)));
  }, [episode.frames]);

  // Playback loop
  useEffect(() => {
    if (!playing) return;

    let lastTime = performance.now();
    const msPerFrame = 1000 / 30; // 30 FPS target

    const loop = (time: number) => {
      const delta = time - lastTime;
      if (delta >= msPerFrame) {
        setFrame((f) => {
          if (f >= episode.frames - 1) {
            setPlaying(false);
            return f;
          }
          return f + 1;
        });
        lastTime = time - (delta % msPerFrame);
      }
      rafRef.current = requestAnimationFrame(loop);
    };

    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [playing, episode.frames]);

  return {
    playing,
    frame,
    progress: episode.frames > 1 ? frame / (episode.frames - 1) : 0,
    togglePlayback,
    seek,
  };
}
