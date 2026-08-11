import time
import uuid
from pathlib import Path

from dotenv import load_dotenv
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse

from .audio_service import LiveAudioService
from .openrouter_service import OpenRouterService, OpenRouterUnavailable, PERSONA_GESTURES
from .schemas import HealthResponse, LiveInterviewManifest, QuestionItem, RoleInterviewRequest


BACKEND_DIR = Path(__file__).resolve().parent.parent
RUNTIME_ROOT = BACKEND_DIR / "runtime_interviews"
PERSONA_NAMES = {
    "warm": "Warm & Encouraging Interviewer",
    "neutral": "Neutral & Professional Interviewer",
    "stern": "Stern & Challenging Interviewer",
}

load_dotenv(BACKEND_DIR / ".env")
RUNTIME_ROOT.mkdir(parents=True, exist_ok=True)

app = FastAPI(title="V-STIPA Live Interview API", version="1.0.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Content-Type"],
)

question_service = OpenRouterService()
audio_service = LiveAudioService(RUNTIME_ROOT)


def get_question_service() -> OpenRouterService:
    return question_service


def get_audio_service() -> LiveAudioService:
    return audio_service


def cleanup_old_sessions(max_age_seconds: int = 86_400) -> None:
    cutoff = time.time() - max_age_seconds
    for path in RUNTIME_ROOT.iterdir():
        if not path.is_dir() or path.stat().st_mtime >= cutoff:
            continue
        for child in path.iterdir():
            if child.is_file():
                child.unlink()
        path.rmdir()


@app.get("/health", response_model=HealthResponse)
async def health(service: OpenRouterService = Depends(get_question_service)) -> HealthResponse:
    return HealthResponse(openrouter_configured=service.configured, model=service.model)


@app.post("/api/interviews", response_model=LiveInterviewManifest)
async def create_interview(
    payload: RoleInterviewRequest,
    request: Request,
    service: OpenRouterService = Depends(get_question_service),
) -> LiveInterviewManifest:
    cleanup_old_sessions()
    try:
        generated, resolved_model = await service.generate_questions(payload.role, payload.persona)
    except OpenRouterUnavailable as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    session_id = uuid.uuid4().hex
    audio_base = f"{str(request.base_url).rstrip('/')}/api/interviews/{session_id}/audio"
    allowed_gestures = PERSONA_GESTURES[payload.persona]
    questions = [
        QuestionItem(
            id=index,
            question=item.question,
            tone=payload.persona,
            gesture=item.gesture
            if item.gesture in allowed_gestures
            else allowed_gestures[(index - 1) % len(allowed_gestures)],
            audio_file=f"{audio_base}/q{index:02d}.wav",
        )
        for index, item in enumerate(generated.questions, start=1)
    ]
    manifest = LiveInterviewManifest(
        persona=payload.persona,
        persona_name=f"{PERSONA_NAMES[payload.persona]} — {payload.role}",
        role=payload.role,
        model=resolved_model,
        session_id=session_id,
        questions=questions,
    )
    session_dir = RUNTIME_ROOT / session_id
    session_dir.mkdir(parents=True, exist_ok=False)
    (session_dir / "manifest.json").write_text(manifest.model_dump_json(indent=2), encoding="utf-8")
    return manifest


@app.get("/api/interviews/{session_id}/audio/q{question_number}.wav")
async def interview_audio(
    session_id: str,
    question_number: int,
    service: LiveAudioService = Depends(get_audio_service),
) -> FileResponse:
    invalid_session_id = len(session_id) != 32 or any(
        character not in "0123456789abcdef" for character in session_id
    )
    if invalid_session_id:
        raise HTTPException(status_code=404, detail="Interview session not found")
    try:
        audio_path = await service.render(session_id, question_number)
    except FileNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except RuntimeError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    return FileResponse(audio_path, media_type="audio/wav", filename=audio_path.name)
