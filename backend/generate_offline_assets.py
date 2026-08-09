import json
import math
import struct
import wave
from pathlib import Path

PERSONAS = {
    "warm": {
        "name": "Warm & Encouraging Interviewer",
        "tone": "warm",
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

def generate_tone_wav(output_path: Path, duration_sec: float = 2.5, freq: float = 440.0):
    sample_rate = 22050
    num_samples = int(sample_rate * duration_sec)

    with wave.open(str(output_path), "w") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)

        for i in range(num_samples):
            t = float(i) / sample_rate
            sample = int(32767.0 * 0.3 * math.sin(2.0 * math.pi * freq * t))
            wav_file.writeframesraw(struct.pack("<h", sample))

def generate_baked_content():
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

            q_items.append({
                "question": q_text,
                "tone": persona_id,
                "gesture": gesture
            })

            freq = 350.0 if persona_id == "warm" else (500.0 if persona_id == "stern" else 440.0)
            generate_tone_wav(audio_path, duration_sec=2.0 + (idx % 3) * 0.5, freq=freq + idx * 10)

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

        print(f"Generated offline assets for persona [{persona_id}]: {len(manifest_entries)} questions in {persona_streaming_dir}")

if __name__ == "__main__":
    generate_baked_content()
