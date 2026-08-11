import asyncio
import json
from pathlib import Path

import edge_tts


VOICE_SETTINGS = {
    "warm": ("en-US-AndrewNeural", "-5%", "+2Hz"),
    "neutral": ("en-US-AndrewNeural", "+0%", "+0Hz"),
    "stern": ("en-US-AndrewNeural", "+5%", "-4Hz"),
}


class LiveAudioService:
    def __init__(self, runtime_root: Path):
        self.runtime_root = runtime_root
        self._locks: dict[str, asyncio.Lock] = {}

    async def render(self, session_id: str, question_number: int) -> Path:
        session_dir = self.runtime_root / session_id
        manifest_path = session_dir / "manifest.json"
        if not manifest_path.is_file():
            raise FileNotFoundError("Interview session does not exist")

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        questions = manifest.get("questions", [])
        if question_number < 1 or question_number > len(questions):
            raise FileNotFoundError("Question does not exist")

        wav_path = session_dir / f"q{question_number:02d}.wav"
        if wav_path.is_file():
            return wav_path

        lock_key = f"{session_id}:{question_number}"
        lock = self._locks.setdefault(lock_key, asyncio.Lock())
        async with lock:
            if wav_path.is_file():
                return wav_path

            voice, rate, pitch = VOICE_SETTINGS[manifest["persona"]]
            temporary_mp3 = session_dir / f"q{question_number:02d}.source.mp3"
            temporary_wav = session_dir / f"q{question_number:02d}.source.wav"
            communicate = edge_tts.Communicate(
                questions[question_number - 1]["question"],
                voice,
                rate=rate,
                pitch=pitch,
            )
            await communicate.save(str(temporary_mp3))
            process = await asyncio.create_subprocess_exec(
                "ffmpeg",
                "-y",
                "-loglevel",
                "error",
                "-i",
                str(temporary_mp3),
                "-ac",
                "1",
                "-ar",
                "24000",
                "-c:a",
                "pcm_s16le",
                str(temporary_wav),
            )
            return_code = await process.wait()
            temporary_mp3.unlink(missing_ok=True)
            if return_code != 0 or not temporary_wav.is_file():
                temporary_wav.unlink(missing_ok=True)
                raise RuntimeError("Male voice rendering failed")
            temporary_wav.replace(wav_path)
            return wav_path
