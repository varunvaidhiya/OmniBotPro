"""Episode recorder — captures state/action frames from a Robot.

The recorder wraps the robot's ``drive`` and ``move_joints`` methods to intercept
commands (the "action"), and polls ``telemetry()`` for the "state". Frames are
captured either on a timer (live) or manually (deterministic tests / sim).

Frame schema (matches LeRobot v2.0 + the mobile-manipulation spec):
  - ``observation.state``: arm joints (6) + base velocity (3) = 9-D
  - ``action``: commanded arm joints (6) + base velocity (3) = 9-D
  - ``timestamp``, ``frame_index``, ``episode_index``, ``task_index``, ``next.done``
"""

from __future__ import annotations

import time
import threading
from dataclasses import dataclass, field
from typing import Dict, List, Optional

from .. import capabilities as caps
from ..robot import Robot


@dataclass
class Frame:
    """One recorded timestep."""

    observation_state: List[float]
    action: List[float]
    timestamp: float
    frame_index: int
    episode_index: int
    task_index: int
    next_done: bool = False


@dataclass
class Episode:
    """A recorded episode — a list of frames plus metadata."""

    task: str = ""
    frames: List[Frame] = field(default_factory=list)
    episode_index: int = 0
    task_index: int = 0
    fps: float = 10.0

    @property
    def length(self) -> int:
        return len(self.frames)

    @property
    def state_dim(self) -> int:
        return len(self.frames[0].observation_state) if self.frames else 0

    @property
    def action_dim(self) -> int:
        return len(self.frames[0].action) if self.frames else 0


def _telemetry_to_state(robot: Robot) -> List[float]:
    """Extract the observation state vector from telemetry.

    Mobile-manipulation 9-D: arm joints (6) + base velocity (3).
    Base-only 3-D: base velocity (vx, vy, omega).
    """
    t = robot.telemetry()
    arm = [j.position for j in t.joints] if t.joints else []
    base_vel = [t.odom.vx, t.odom.vy, t.odom.omega] if t.odom else [0.0, 0.0, 0.0]
    if robot.has(caps.MANIPULATION):
        return arm + base_vel
    return base_vel


class Recorder:
    """Records episodes from a :class:`Robot`.

    Usage (manual / sim)::

        rec = Recorder(bot, repo_id="local/demo")
        rec.start_episode(task="pick up the cup")
        for _ in range(50):
            bot.drive(vx=0.1)
            rec.capture_frame()
        rec.stop_episode()
        rec.save("~/datasets/demo")

    Usage (live / timer)::

        rec = Recorder(bot, repo_id="local/demo", fps=10.0)
        rec.record_episode(task="pick up the cup")  # background thread
        # ...teleop...
        rec.stop_episode()
        rec.save("~/datasets/demo")
    """

    def __init__(
        self,
        robot: Robot,
        repo_id: str = "local/episodes",
        fps: float = 10.0,
    ) -> None:
        self.robot = robot
        self.repo_id = repo_id
        self.fps = fps
        self._episodes: List[Episode] = []
        self._current: Optional[Episode] = None
        self._last_action: List[float] = self._zero_action()
        self._frame_idx = 0
        self._task_map: Dict[str, int] = {}
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()
        self._t0: Optional[float] = None
        self._wrap_robot()

    def _zero_action(self) -> List[float]:
        """Zero action vector sized to the robot's DOF."""
        n_arm = (
            len(self.robot.spec.joint_names) if self.robot.has(caps.MANIPULATION) else 0
        )
        return [0.0] * (n_arm + 3)

    def _wrap_robot(self) -> None:
        """Intercept drive/move_joints to capture the action."""
        original_drive = self.robot.drive
        original_move_joints = self.robot.move_joints

        recorder = self

        def drive(vx: float = 0.0, vy: float = 0.0, w: float = 0.0):
            action = recorder._last_action[:]
            n_arm = (
                len(recorder.robot.spec.joint_names)
                if recorder.robot.has(caps.MANIPULATION)
                else 0
            )
            action[n_arm:] = [vx, vy, w]
            recorder._last_action = action
            return original_drive(vx, vy, w)

        def move_joints(positions):
            positions = list(positions)
            action = recorder._last_action[:]
            n_arm = min(len(positions), len(action) - 3)
            action[:n_arm] = positions[:n_arm]
            recorder._last_action = action
            return original_move_joints(positions)

        self.robot.drive = drive  # type: ignore[method-assign]
        self.robot.move_joints = move_joints  # type: ignore[method-assign]

    # ── manual recording ─────────────────────────────────────────────────────
    def start_episode(self, task: str = "") -> Episode:
        """Begin a new episode (manual mode)."""
        if self._current is not None:
            raise RuntimeError("an episode is already recording — stop it first")
        task_index = self._task_map.get(task, len(self._task_map))
        self._task_map[task] = task_index
        self._current = Episode(
            task=task,
            episode_index=len(self._episodes),
            task_index=task_index,
            fps=self.fps,
        )
        self._frame_idx = 0
        self._t0 = time.monotonic()
        self._last_action = self._zero_action()
        return self._current

    def capture_frame(self) -> Frame:
        """Capture one frame (manual mode). Must be called after start_episode."""
        if self._current is None:
            raise RuntimeError("no active episode — call start_episode() first")
        state = _telemetry_to_state(self.robot)
        action = self._last_action[:]
        ts = time.monotonic() - (self._t0 or 0.0)
        frame = Frame(
            observation_state=state,
            action=action,
            timestamp=ts,
            frame_index=self._frame_idx,
            episode_index=self._current.episode_index,
            task_index=self._current.task_index,
        )
        self._current.frames.append(frame)
        self._frame_idx += 1
        return frame

    def stop_episode(self) -> Episode:
        """Finish the current episode and store it."""
        if self._current is None:
            raise RuntimeError("no active episode")
        if self._current.frames:
            self._current.frames[-1].next_done = True
        ep = self._current
        self._episodes.append(ep)
        self._current = None
        return ep

    def discard_episode(self) -> None:
        """Discard the current episode without saving."""
        self._current = None

    # ── timer recording ──────────────────────────────────────────────────────
    def record_episode(self, task: str = "") -> Episode:
        """Start a background-thread episode at ``fps`` Hz."""
        self.start_episode(task)
        self._stop.clear()
        period = 1.0 / self.fps

        def loop():
            while not self._stop.is_set() and self._current is not None:
                self.capture_frame()
                time.sleep(period)

        self._thread = threading.Thread(target=loop, name="ohho-recorder", daemon=True)
        self._thread.start()
        return self._current

    def stop_recording(self) -> Episode:
        """Stop the background thread and finish the episode."""
        self._stop.set()
        if self._thread is not None:
            self._thread.join(timeout=1.0)
            self._thread = None
        return self.stop_episode()

    # ── output ───────────────────────────────────────────────────────────────
    @property
    def episodes(self) -> List[Episode]:
        return list(self._episodes)

    @property
    def total_frames(self) -> int:
        return sum(ep.length for ep in self._episodes)

    @property
    def tasks(self) -> List[str]:
        return sorted(self._task_map, key=lambda t: self._task_map[t])

    def save(self, path: str) -> str:
        """Write all episodes to ``path`` in LeRobot v2.0 format.

        Uses Parquet when ``pyarrow`` is available (the ``[data]`` extra),
        otherwise falls back to JSON Lines. Meta files (``info.json``,
        ``episodes.jsonl``, ``tasks.jsonl``) are always JSON.
        """
        from .writer import write_dataset

        return write_dataset(self._episodes, path, self.repo_id, self.fps, self.tasks)
