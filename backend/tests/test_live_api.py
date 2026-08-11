import asyncio
import json
import wave

from fastapi.testclient import TestClient

from app import main
from app.audio_service import VOICE_SETTINGS
from app.openrouter_service import OpenRouterService, OpenRouterUnavailable
from app.schemas import GeneratedQuestion, GeneratedQuestionSet


class FakeQuestionService:
    configured = True
    model = "test/role-model"

    async def generate_questions(self, role: str, persona: str):
        questions = GeneratedQuestionSet(
            questions=[
                GeneratedQuestion(
                    question=(
                        f"For the {role} position, how would you handle "
                        f"realistic scenario number {index}?"
                    ),
                    gesture="nod",
                )
                for index in range(1, 13)
            ]
        )
        return questions, self.model


class FailingQuestionService:
    configured = False
    model = "openrouter/free"

    async def generate_questions(self, role: str, persona: str):
        raise OpenRouterUnavailable("OPENROUTER_API_KEY is not configured")


class FakeAudioService:
    def __init__(self, output_path):
        self.output_path = output_path

    async def render(self, session_id: str, question_number: int):
        with wave.open(str(self.output_path), "wb") as wav_file:
            wav_file.setnchannels(1)
            wav_file.setsampwidth(2)
            wav_file.setframerate(24000)
            wav_file.writeframes(b"\0\0" * 2400)
        return self.output_path


def test_live_tones_use_one_male_voice_identity():
    assert {settings[0] for settings in VOICE_SETTINGS.values()} == {
        "en-US-AndrewNeural",
    }
    assert len({settings[1:] for settings in VOICE_SETTINGS.values()}) == 3


def test_role_interview_returns_twelve_role_specific_questions(tmp_path, monkeypatch):
    monkeypatch.setattr(main, "RUNTIME_ROOT", tmp_path)
    main.app.dependency_overrides[main.get_question_service] = lambda: FakeQuestionService()
    try:
        response = TestClient(main.app).post(
            "/api/interviews",
            json={"role": "Senior Backend Engineer", "persona": "warm"},
        )
    finally:
        main.app.dependency_overrides.clear()

    assert response.status_code == 200
    manifest = response.json()
    assert manifest["role"] == "Senior Backend Engineer"
    assert manifest["source"] == "openrouter"
    assert manifest["model"] == "test/role-model"
    assert len(manifest["questions"]) == 12
    assert all("Senior Backend Engineer" in item["question"] for item in manifest["questions"])
    assert manifest["questions"][0]["audio_file"].endswith("/audio/q01.wav")


def test_missing_key_returns_service_unavailable():
    main.app.dependency_overrides[main.get_question_service] = lambda: FailingQuestionService()
    try:
        response = TestClient(main.app).post(
            "/api/interviews",
            json={"role": "Product Designer", "persona": "neutral"},
        )
    finally:
        main.app.dependency_overrides.clear()

    assert response.status_code == 503
    assert "OPENROUTER_API_KEY" in response.json()["detail"]


def test_live_audio_endpoint_returns_pcm_wav(tmp_path):
    session_id = "a" * 32
    output_path = tmp_path / "q01.wav"
    main.app.dependency_overrides[main.get_audio_service] = lambda: FakeAudioService(output_path)
    try:
        response = TestClient(main.app).get(f"/api/interviews/{session_id}/audio/q01.wav")
    finally:
        main.app.dependency_overrides.clear()

    assert response.status_code == 200
    assert response.headers["content-type"] == "audio/wav"
    assert response.content.startswith(b"RIFF")


def test_openrouter_service_sends_role_and_parses_structured_questions(monkeypatch):
    captured = {}
    response_content = {
        "questions": [
            {
                "question": f"How would you solve role-specific engineering scenario {index}?",
                "gesture": "nod",
            }
            for index in range(1, 13)
        ]
    }

    class FakeResponse:
        status_code = 200
        is_error = False
        headers = {}

        def json(self):
            return {
                "model": "provider/test-model",
                "choices": [{"message": {"content": json.dumps(response_content)}}],
            }

    class FakeAsyncClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, exc_type, exc, traceback):
            return False

        async def post(self, url, headers, json):
            captured.update({"url": url, "headers": headers, "payload": json})
            return FakeResponse()

    monkeypatch.setattr("app.openrouter_service.httpx.AsyncClient", FakeAsyncClient)
    service = OpenRouterService(api_key="test-key", model="openrouter/free")
    questions, model = asyncio.run(service.generate_questions("Platform Engineer", "warm"))

    assert len(questions.questions) == 12
    assert model == "provider/test-model"
    assert captured["payload"]["messages"][1]["content"] == "Target position: Platform Engineer"
    assert captured["payload"]["response_format"]["type"] == "json_schema"
    assert captured["headers"]["Authorization"] == "Bearer test-key"
