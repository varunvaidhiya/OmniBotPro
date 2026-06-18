"""WorkingMemory — bridges long- and short-term memory into reasoning.

Fixes the gap where ``EntityMemory`` exists but is never injected into the
LLM's context. Each planning cycle, :meth:`context_for` assembles:

  * long-term symbolic memory — ``EntityStore.get_summary()`` (objects and the
    rooms they were last seen in), or the snapshot's ``memory_summary`` when no
    store is wired;
  * short-term memory — outcomes of the last few goals this session.

On episode end, :meth:`record_outcome` appends to the session log and writes
any newly-grounded objects back to the long-term store. The episodic
``ReplayDataset`` write-back (for continual learning) is handled separately by
the ROS ``episode_logger`` node — this class owns the symbolic memory only.
"""

from __future__ import annotations

from collections import deque
from typing import Callable, Deque, Optional, Tuple

from ..core.blackboard import WorldState
from ..core.interfaces import EntityStore
from ..core.types import Goal, Reflection


class WorkingMemory:
    def __init__(
        self,
        entity_store: Optional[EntityStore] = None,
        max_recent: int = 5,
        episode_source: Optional[Callable[[], str]] = None,
    ) -> None:
        self._entity = entity_store
        self._episode_source = episode_source
        # (goal_text, success, summary)
        self._recent: Deque[Tuple[str, bool, str]] = deque(maxlen=max_recent)

    def context_for(self, goal: Goal, world: WorldState) -> str:
        parts = []
        summary = self._entity.get_summary() if self._entity else world.memory_summary
        if summary and summary != "No objects remembered yet.":
            parts.append("Known objects and places:\n" + summary)
        if self._recent:
            lines = [
                "- {} -> {}: {}".format(text, "succeeded" if ok else "failed", note)
                for text, ok, note in self._recent
            ]
            parts.append("Recent goal outcomes this session:\n" + "\n".join(lines))
        if self._episode_source is not None:
            past = self._episode_source()
            if past:
                parts.append(past)
        return "\n\n".join(parts)

    def record_outcome(self, goal: Goal, reflection: Reflection) -> None:
        self._recent.append((goal.text, reflection.success, reflection.summary))
        if self._entity is not None:
            for name, location in reflection.learned_objects.items():
                self._entity.remember_object(name, location, "")
