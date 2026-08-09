import json
from unittest.mock import MagicMock
from generate_questions import generate_questions_for_persona, QuestionList

def test_mocked_gemini_generation():
    mock_client = MagicMock()
    mock_response = MagicMock()

    mock_questions = {
        "questions": [
            {
                "question": f"Question #{i}: Describe your architecture decision.",
                "tone": "neutral",
                "gesture": "thinking"
            }
            for i in range(1, 13)
        ]
    }

    mock_response.text = json.dumps(mock_questions)
    mock_client.models.generate_content.return_value = mock_response

    persona = {
        "persona_id": "neutral",
        "system_prompt": "You are a neutral interviewer."
    }

    result = generate_questions_for_persona(persona, mock_client)

    assert isinstance(result, QuestionList)
    assert len(result.questions) == 12
    assert result.questions[0].tone == "neutral"
    assert mock_client.models.generate_content.called
