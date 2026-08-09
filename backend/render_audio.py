import json
import os
import sys
import yaml
from pathlib import Path
from dotenv import load_dotenv
from google.cloud import texttospeech

load_dotenv()

def render_audio_for_persona(persona_id: str, backend_dir: Path):
    personas_dir = backend_dir / "personas"
    output_dir = backend_dir / "output"
    streaming_dir = backend_dir.parent / "unity-project" / "Assets" / "StreamingAssets" / "questions" / persona_id
    streaming_dir.mkdir(parents=True, exist_ok=True)

    persona_path = personas_dir / f"{persona_id}.yaml"
    questions_path = output_dir / f"questions_{persona_id}.json"

    if not persona_path.exists() or not questions_path.exists():
        print(f"[{persona_id}] Missing persona config or questions JSON file.")
        return

    with open(persona_path, "r", encoding="utf-8") as f:
        persona = yaml.safe_load(f)

    with open(questions_path, "r", encoding="utf-8") as f:
        q_data = json.load(f)

    voice_name = persona.get("tts_voice_name", "en-US-Wavenet-C")
    speaking_rate = persona.get("speaking_rate", 1.0)
    pitch = persona.get("pitch", 0.0)

    client = texttospeech.TextToSpeechClient()
    manifest_entries = []

    for idx, item in enumerate(q_data.get("questions", []), start=1):
        q_text = item["question"]
        tone = item["tone"]
        gesture = item["gesture"]

        audio_filename = f"q{idx:02d}.mp3"
        audio_path = streaming_dir / audio_filename

        print(f"[{persona_id}] Rendering audio for Q{idx:02d} ({len(q_text)} chars)...")
        synthesis_input = texttospeech.SynthesisInput(text=q_text)
        voice = texttospeech.VoiceSelectionParams(
            language_code="en-US",
            name=voice_name
        )
        audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.MP3,
            speaking_rate=speaking_rate,
            pitch=pitch
        )

        response = client.synthesize_speech(
            input=synthesis_input,
            voice=voice,
            audio_config=audio_config
        )

        with open(audio_path, "wb") as out_f:
            out_f.write(response.audio_content)

        manifest_entries.append({
            "id": idx,
            "question": q_text,
            "tone": tone,
            "gesture": gesture,
            "audio_file": audio_filename
        })

    manifest_path = streaming_dir / "manifest.json"
    manifest_data = {
        "persona": persona_id,
        "persona_name": persona.get("name", persona_id),
        "total_questions": len(manifest_entries),
        "questions": manifest_entries
    }

    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest_data, f, indent=2)

    print(f"[{persona_id}] Manifest written: {manifest_path}")

def main():
    backend_dir = Path(__file__).parent
    output_dir = backend_dir / "output"

    for q_file in output_dir.glob("questions_*.json"):
        persona_id = q_file.stem.replace("questions_", "")
        render_audio_for_persona(persona_id, backend_dir)

if __name__ == "__main__":
    main()
