"""Language-goal evaluation: did the robot complete the natural-language
instruction? Produces a completion confidence in [0, 1].

Uses sampled camera frames + the instruction with a VLM judge. The outcome
thresholds (success ≥ 0.8, near-success ≥ 0.5) are shared with the
self-evaluation layer via ``classify_outcome``.
"""

from __future__ import annotations

import json
import re
from typing import List, Optional

import numpy as np

from ..core.interfaces import EpisodeEvaluator, VLMClient
from ..core.registry import EVALUATORS
from ..core.types import Episode, EvaluationReport, TaskOutcome
from ..data import schema
from ..rewards.vision import sample_frames

SUCCESS_THRESHOLD = 0.8
NEAR_SUCCESS_THRESHOLD = 0.5

_JUDGE_PROMPT = """A robot was given this instruction: "{instruction}"

You see {n} frames from the robot's camera in chronological order
(start → end of episode).

Answer in JSON only:
{{"confidence": <0.0-1.0 how confident you are the instruction was completed>,
  "rationale": "<one sentence>"}}"""


def classify_outcome(confidence: float) -> TaskOutcome:
    if confidence >= SUCCESS_THRESHOLD:
        return TaskOutcome.SUCCESS
    if confidence >= NEAR_SUCCESS_THRESHOLD:
        return TaskOutcome.NEAR_SUCCESS
    return TaskOutcome.FAILURE


def episode_frames(
    episode: Episode, max_frames: int = 4, camera_priority: Optional[List[str]] = None
) -> List[np.ndarray]:
    """Pull the best available camera stream from an episode."""
    cameras = camera_priority or [
        schema.OBS_IMAGE_WRIST,
        schema.OBS_IMAGE_FRONT,
        schema.OBS_IMAGE_BEV,
    ]
    for cam in cameras:
        frames = [s.observation[cam] for s in episode.steps if cam in s.observation]
        if frames:
            return list(sample_frames(frames, max_frames))
    return []


@EVALUATORS.register("language_goal")
class LanguageGoalEvaluator(EpisodeEvaluator):
    def __init__(self, client: VLMClient, max_frames: int = 4) -> None:
        self.client = client
        self.max_frames = max_frames

    def evaluate(self, episode: Episode) -> EvaluationReport:
        instruction = episode.meta.task_instruction
        frames = episode_frames(episode, self.max_frames)
        if not instruction or not frames:
            return EvaluationReport(
                episode_id=episode.meta.episode_id,
                completion_confidence=0.0,
                outcome=TaskOutcome.UNKNOWN,
                task_summary="no instruction or no camera frames — cannot judge",
                judge="language_goal",
            )
        reply = self.client.complete(
            _JUDGE_PROMPT.format(instruction=instruction, n=len(frames)),
            images=frames,
        )
        confidence, rationale = _parse_reply(reply)
        return EvaluationReport(
            episode_id=episode.meta.episode_id,
            completion_confidence=confidence,
            outcome=classify_outcome(confidence),
            task_summary=rationale,
            judge="language_goal",
        )


def _parse_reply(reply: str) -> tuple:
    try:
        m = re.search(r"\{.*\}", reply, re.DOTALL)
        doc = json.loads(m.group()) if m else {}
        return (
            float(np.clip(float(doc.get("confidence", 0.0)), 0.0, 1.0)),
            str(doc.get("rationale", "")),
        )
    except (ValueError, TypeError):
        return 0.0, f"unparseable judge reply: {reply[:120]}"
