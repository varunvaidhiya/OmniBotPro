/*
 * OhhO Pilot — Episode recorder.
 *
 * Wraps any SimulatorProvider to capture 9-DOF mobile-manipulation
 * episodes in LeRobot-compatible CSV format. Use for building training
 * datasets from simulated or real robot teleop sessions.
 */

import type {
  SimulatorProvider,
  RecordingFrame,
  RecordedEpisode,
} from "./types";

/**
 * Attach recording capability to a provider. Returns controls and
 * subscribes to provider data streams while recording.
 */
export function createRecorder(provider: SimulatorProvider) {
  let _frames: RecordingFrame[] = [];
  let _startedAt = 0;
  const _unsubs: Array<() => void> = [];

  function start(instruction = "manual teleop episode"): void {
    _frames = [];
    _startedAt = Date.now();
    provider.startRecording(instruction);

    // Subscribe to odometry to capture base velocities
    const unsubOdom = provider.onOdometry((odom) => {
      if (!provider.isRecording()) return;
      const frame = _frames[_frames.length - 1];
      if (frame) {
        frame.baseVelocity = [odom.linearX, odom.linearY, odom.angularZ];
      }
    });
    _unsubs.push(unsubOdom);

    // Subscribe to joint states to capture arm positions
    const unsubJoints = provider.onJointStates((joints) => {
      if (!provider.isRecording()) return;
      const armJoints = joints.slice(0, 6);
      const frame: RecordingFrame = {
        timestamp: Date.now(),
        armPositions: armJoints.map((j) => j.position),
        baseVelocity: [0, 0, 0],
        cameraFrames: {},
      };
      _frames.push(frame);
    });
    _unsubs.push(unsubJoints);
  }

  function stop(): RecordedEpisode | null {
    // Clean up subscriptions
    for (const unsub of _unsubs) unsub();
    _unsubs.length = 0;

    const episode = provider.stopRecording();
    if (!episode) return null;

    // Augment with our captured frames
    return {
      ...episode,
      frameCount: _frames.length || episode.frameCount,
    };
  }

  function download(episode: RecordedEpisode): void {
    const blob = new Blob([episode.exportContent], {
      type: "text/csv;charset=utf-8",
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${episode.id}_${episode.provider}_episode.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  return { start, stop, download };
}
