"""Connector configuration — all values come from environment variables."""

from __future__ import annotations

import os
from dataclasses import dataclass, field


@dataclass
class ConnectorConfig:
    rosbridge_url: str = field(
        default_factory=lambda: os.getenv("ROSBRIDGE_URL", "ws://localhost:9090")
    )
    rest_port: int = field(
        default_factory=lambda: int(os.getenv("CONNECTOR_REST_PORT", "8080"))
    )
    mcp_port: int = field(
        default_factory=lambda: int(os.getenv("CONNECTOR_MCP_PORT", "8081"))
    )
    stream_port: int = field(
        default_factory=lambda: int(os.getenv("CONNECTOR_STREAM_PORT", "8082"))
    )
    api_key: str = field(default_factory=lambda: os.getenv("CONNECTOR_API_KEY", ""))
    locations_yaml: str = field(default_factory=lambda: os.getenv("LOCATIONS_YAML", ""))
    max_lin_vel: float = field(
        default_factory=lambda: float(os.getenv("MAX_LIN_VEL", "0.20"))
    )
    max_ang_vel: float = field(
        default_factory=lambda: float(os.getenv("MAX_ANG_VEL", "1.00"))
    )
    anthropic_api_key: str = field(
        default_factory=lambda: os.getenv("ANTHROPIC_API_KEY", "")
    )
    vla_serve_url: str = field(
        default_factory=lambda: os.getenv("VLA_SERVE_URL", "http://localhost:8000")
    )
    episode_dir: str = field(
        default_factory=lambda: os.getenv("EPISODE_DIR", "~/.omnibot/episodes")
    )
    camera_fps: int = field(
        default_factory=lambda: int(os.getenv("CAMERA_STREAM_FPS", "2"))
    )
    voice_wake_word: str = field(
        default_factory=lambda: os.getenv("VOICE_WAKE_WORD", "hey omnibot")
    )
    whisper_model: str = field(
        default_factory=lambda: os.getenv("WHISPER_MODEL", "base.en")
    )
    whisper_device: str = field(
        default_factory=lambda: os.getenv("WHISPER_DEVICE", "cpu")
    )


_cfg: ConnectorConfig | None = None


def get_config() -> ConnectorConfig:
    global _cfg
    if _cfg is None:
        _cfg = ConnectorConfig()
    return _cfg
