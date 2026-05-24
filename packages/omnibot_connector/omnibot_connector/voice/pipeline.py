"""Voice pipeline: openWakeWord → faster-whisper → ROS /ai/command + WS audio stream."""

from __future__ import annotations

import asyncio
import logging
import threading
from typing import Optional

import numpy as np

logger = logging.getLogger(__name__)


class VoicePipeline:
    def __init__(self, cfg, ros, audio_event_push_fn) -> None:
        self._cfg = cfg
        self._ros = ros
        self._push = audio_event_push_fn
        self._active_mode = False
        self._stop = threading.Event()
        self._loop: Optional[asyncio.AbstractEventLoop] = None

    def set_active_mode(self, active: bool) -> None:
        self._active_mode = active
        logger.info(
            "Voice pipeline mode: %s",
            "ACTIVE (24/7)" if active else "PASSIVE (wake word only)",
        )

    def start(self) -> None:
        self._loop = asyncio.get_event_loop()
        t = threading.Thread(target=self._mic_loop, daemon=True, name="voice-pipeline")
        t.start()

    def stop(self) -> None:
        self._stop.set()

    def _mic_loop(self) -> None:
        try:
            import sounddevice as sd
            from faster_whisper import WhisperModel
            from openwakeword.model import Model

            oww = Model(wakeword_models=[self._cfg.voice_wake_word])
            whisper = WhisperModel(
                self._cfg.whisper_model, device=self._cfg.whisper_device
            )
            CHUNK = 1280  # 80 ms at 16 kHz
            SILENCE_THRESHOLD = 0.01
            SILENCE_FRAMES_REQUIRED = int(1.5 * 16000 / CHUNK)
            MIN_AUDIO_FRAMES = 5

            audio_buf: list = []
            listening = False
            silence_frames = 0

            def callback(indata, frames, time_info, status):
                nonlocal listening, silence_frames, audio_buf
                chunk = indata[:, 0].astype(np.float32)
                oww.predict(chunk)
                scores = oww.prediction_buffer
                wake_score = max((v[-1] for v in scores.values()), default=0.0)
                if wake_score > 0.5 and not listening:
                    listening = True
                    silence_frames = 0
                    audio_buf = []
                    logger.info("Wake word detected!")
                if listening or self._active_mode:
                    audio_buf.append(chunk.copy())
                    rms = float(np.sqrt(np.mean(chunk**2)))
                    if rms < SILENCE_THRESHOLD:
                        silence_frames += 1
                    else:
                        silence_frames = 0
                    if (
                        silence_frames >= SILENCE_FRAMES_REQUIRED
                        and len(audio_buf) > MIN_AUDIO_FRAMES
                    ):
                        audio = np.concatenate(audio_buf)
                        audio_buf = []
                        listening = False
                        silence_frames = 0
                        self._transcribe(whisper, audio)

            with sd.InputStream(
                samplerate=16000,
                channels=1,
                dtype="float32",
                blocksize=CHUNK,
                callback=callback,
            ):
                logger.info(
                    "Voice pipeline listening (wake word: '%s')",
                    self._cfg.voice_wake_word,
                )
                while not self._stop.is_set():
                    import time

                    time.sleep(0.1)
        except ImportError:
            logger.warning(
                "Voice deps not installed. Run: pip install 'omnibot-connector[voice]'"
            )
        except Exception as exc:
            logger.error("Voice pipeline error: %s", exc)

    def _transcribe(self, whisper, audio: np.ndarray) -> None:
        try:
            segments, _ = whisper.transcribe(audio, language="en")
            text = " ".join(s.text.strip() for s in segments).strip()
            if not text:
                return
            logger.info("Transcribed: %s", text)
            import time

            event = {"type": "voice_command", "text": text, "timestamp": time.time()}
            if self._push:
                self._push(event)
            if self._ros and self._loop:
                asyncio.run_coroutine_threadsafe(
                    self._ros.publish("/ai/command", {"data": text}),
                    self._loop,
                )
        except Exception as exc:
            logger.error("Transcription error: %s", exc)


def main() -> None:
    import asyncio

    from omnibot_connector.config import get_config

    logging.basicConfig(level=logging.INFO)
    cfg = get_config()
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)
    pipeline = VoicePipeline(cfg, None, None)
    pipeline.set_active_mode(True)
    pipeline.start()
    loop.run_forever()
