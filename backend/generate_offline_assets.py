import asyncio
import json
import subprocess
from pathlib import Path

import edge_tts

PERSONAS = {
    "warm": {
        "name": "Warm & Encouraging Interviewer",
        "tone": "warm",
        "voice": "en-US-AndrewNeural",
        "rate": "-5%",
        "pitch": "+2Hz",
        "gestures": ["nod", "lean_forward", "smile", "idle", "thinking"],
        "questions": [
            "Welcome! To start off, could you tell me about a project you are particularly proud of?",
            "That sounds fascinating. How did you handle collaboration with your team during that effort?",
            "What inspired you to take on that technical approach?",
            "Could you share how you mentored or supported a teammate through a complex bug?",
            "When requirements changed unexpectedly, how did you help keep team morale high?",
            "What is a skill you recently learned that you feel passionate about?",
            "How do you prefer to receive feedback from your lead or peers?",
            "Tell me about a time when a mistake turned into a valuable learning opportunity.",
            "How do you balance technical excellence with team velocity?",
            "What environments bring out your best problem-solving energy?",
            "How do you ensure everyone on your team has a voice during design discussions?",
            "Lastly, what are you most excited to build or learn next?"
        ]
    },
    "stern": {
        "name": "Stern & Challenging Interviewer",
        "tone": "stern",
        "voice": "en-US-ChristopherNeural",
        "rate": "+5%",
        "pitch": "-4Hz",
        "gestures": ["arms_crossed", "lean_back", "nod_firm", "idle", "thinking"],
        "questions": [
            "We have strict performance constraints. Describe a production incident you directly caused or resolved.",
            "Why did you choose that specific architecture over simpler alternatives?",
            "How do you defend your technical decisions when senior engineers strongly disagree?",
            "Describe a scenario where you had to ship under tight deadline pressure with incomplete specs.",
            "What is the single biggest architectural mistake you have made in your career?",
            "How do you handle working with team members who fail to meet performance expectations?",
            "Walk me through how you optimize memory allocations in critical paths.",
            "If your service experiences a 10x traffic spike right now, where will it fail first?",
            "When forced to cut scope to meet a release target, what criteria do you prioritize?",
            "Explain how you debug non-deterministic race conditions under heavy load.",
            "Why should we trust your technical judgment on high-availability system designs?",
            "What trade-offs did you make in your last project that you now regret?"
        ]
    },
    "neutral": {
        "name": "Neutral & Professional Interviewer",
        "tone": "neutral",
        "voice": "en-US-EricNeural",
        "rate": "+0%",
        "pitch": "+0Hz",
        "gestures": ["nod", "idle", "thinking", "lean_forward"],
        "questions": [
            "Please walk me through your background and key technical areas of expertise.",
            "Describe your experience with distributed systems and microservices architecture.",
            "How do you approach writing clean, maintainable, and well-tested code?",
            "Explain a complex data structure or algorithm you implemented recently.",
            "What process do you follow for code reviews and architectural RFCs?",
            "How do you monitor and instrument services running in production environments?",
            "Describe your experience working with cross-functional product and design teams.",
            "How do you evaluate and integrate third-party libraries into an existing codebase?",
            "What strategies do you use for database schema migrations with zero downtime?",
            "Tell me about a time you had to pick up an unfamiliar technology stack quickly.",
            "How do you prioritize tech debt alongside new feature development?",
            "Do you have any questions for me regarding our engineering team or architecture?"
        ]
    }
}

async def synthesize_to_mp3(text: str, voice: str, rate: str, pitch: str, output_path: Path):
    """Render one clip with bounded retries before touching the committed asset."""
    temporary_path = output_path.with_suffix(".source.mp3")
    temporary_path.unlink(missing_ok=True)

    for attempt in range(1, 4):
        try:
            communicate = edge_tts.Communicate(text, voice, rate=rate, pitch=pitch)
            await communicate.save(str(temporary_path))
            temporary_path.replace(output_path)
            return
        except Exception:
            temporary_path.unlink(missing_ok=True)
            if attempt == 3:
                raise
            await asyncio.sleep(attempt * 1.5)


async def generate_baked_content():
    backend_dir = Path(__file__).parent
    output_dir = backend_dir / "output"
    streaming_base = backend_dir.parent / "unity-project" / "Assets" / "StreamingAssets" / "questions"

    output_dir.mkdir(parents=True, exist_ok=True)
    streaming_base.mkdir(parents=True, exist_ok=True)

    for persona_id, pdata in PERSONAS.items():
        gestures = pdata["gestures"]
        q_items = []
        manifest_entries = []

        persona_streaming_dir = streaming_base / persona_id
        persona_streaming_dir.mkdir(parents=True, exist_ok=True)

        for idx, q_text in enumerate(pdata["questions"], start=1):
            gesture = gestures[(idx - 1) % len(gestures)]
            audio_filename = f"q{idx:02d}.wav"
            audio_path = persona_streaming_dir / audio_filename
            mp3_path = persona_streaming_dir / f"q{idx:02d}.mp3"
            temporary_wav = persona_streaming_dir / f"q{idx:02d}.source.wav"

            q_items.append({
                "question": q_text,
                "tone": persona_id,
                "gesture": gesture
            })

            print(
                f"[{persona_id}] Synthesizing male voice {pdata['voice']} "
                f"for Q{idx:02d}..."
            )
            await synthesize_to_mp3(
                q_text,
                pdata["voice"],
                pdata["rate"],
                pdata["pitch"],
                mp3_path,
            )
            subprocess.run(
                [
                    "ffmpeg", "-y", "-loglevel", "error", "-i", str(mp3_path),
                    "-ac", "1", "-ar", "24000", "-c:a", "pcm_s16le", str(temporary_wav)
                ],
                check=True
            )
            temporary_wav.replace(audio_path)

            manifest_entries.append({
                "id": idx,
                "question": q_text,
                "tone": persona_id,
                "gesture": gesture,
                "audio_file": audio_filename
            })

        q_file = output_dir / f"questions_{persona_id}.json"
        with open(q_file, "w", encoding="utf-8") as f:
            json.dump({"questions": q_items}, f, indent=2)

        manifest_path = persona_streaming_dir / "manifest.json"
        manifest_data = {
            "persona": persona_id,
            "persona_name": pdata["name"],
            "total_questions": len(manifest_entries),
            "questions": manifest_entries
        }

        with open(manifest_path, "w", encoding="utf-8") as f:
            json.dump(manifest_data, f, indent=2)

        print(f"Generated spoken voice assets for persona [{persona_id}]: {len(manifest_entries)} questions in {persona_streaming_dir}")

if __name__ == "__main__":
    asyncio.run(generate_baked_content())
