from __future__ import annotations

import asyncio
import json
import time
from pathlib import Path


class EpisodeRecorder:
    def __init__(self, cfg) -> None:
        self._dir = Path(cfg.episode_dir).expanduser()
        self._dir.mkdir(parents=True, exist_ok=True)
        self._current_episode: list = []
        self._episode_id = int(time.time())
        self._lock = asyncio.Lock()

    async def start(self) -> None:
        asyncio.create_task(self._periodic_flush())

    async def record_tool_call(self, tool: str, arguments: dict, result: dict) -> None:
        event = {
            "timestamp": time.time(),
            "type": "tool_call",
            "tool": tool,
            "arguments": arguments,
            "result": result,
        }
        async with self._lock:
            self._current_episode.append(event)
        if tool in ("navigate_to_location", "navigate_then_manipulate", "execute_manipulation"):
            await self._flush()

    async def _flush(self) -> None:
        async with self._lock:
            if not self._current_episode:
                return
            path = self._dir / f"episode_{self._episode_id}.json"
            with open(path, "w") as f:
                json.dump(
                    {"episode_id": self._episode_id, "events": self._current_episode},
                    f,
                    indent=2,
                )
            self._current_episode = []
            self._episode_id = int(time.time())

    async def _periodic_flush(self) -> None:
        while True:
            await asyncio.sleep(60)
            await self._flush()
