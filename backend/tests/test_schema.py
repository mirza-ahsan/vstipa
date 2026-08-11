import pytest
import yaml
from pathlib import Path
from generate_offline_assets import PERSONAS
from generate_questions import QuestionItem, QuestionList

def test_persona_yaml_validity():
    personas_dir = Path(__file__).parent.parent / "personas"
    assert personas_dir.exists(), "Personas directory must exist"

    persona_files = list(personas_dir.glob("*.yaml"))
    assert len(persona_files) == 3, f"Expected 3 persona files, found {len(persona_files)}"

    for p_file in persona_files:
        with open(p_file, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)
            assert "persona_id" in data
            assert "name" in data
            assert "system_prompt" in data
            assert "tts_voice_name" in data

            # Google Cloud's published en-US WaveNet catalogue identifies these
            # five voices as male. Keep the baked fallback aligned with the male
            # interviewer avatar if persona settings change later.
            assert data["tts_voice_name"] in {
                "en-US-Wavenet-A",
                "en-US-Wavenet-B",
                "en-US-Wavenet-D",
                "en-US-Wavenet-I",
                "en-US-Wavenet-J",
            }

def test_question_schema_validation():
    valid_data = {
        "questions": [
            {
                "question": "Tell me about a time you resolved a technical conflict.",
                "tone": "warm",
                "gesture": "nod"
            }
        ] * 12
    }

    q_list = QuestionList.model_validate(valid_data)
    assert len(q_list.questions) == 12
    assert q_list.questions[0].gesture == "nod"


def test_offline_fallback_uses_male_neural_voices():
    assert {persona["voice"] for persona in PERSONAS.values()} == {
        "en-US-AndrewNeural",
        "en-US-ChristopherNeural",
        "en-US-EricNeural",
    }

def test_invalid_question_schema():
    invalid_data = {
        "questions": [
            {
                "question": "Missing tone field",
                "gesture": "smile"
            }
        ]
    }
    with pytest.raises(Exception):
        QuestionList.model_validate(invalid_data)
