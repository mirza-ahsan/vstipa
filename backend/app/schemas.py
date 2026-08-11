import re
from typing import Literal

from pydantic import BaseModel, Field, field_validator


PersonaId = Literal["warm", "neutral", "stern"]


class RoleInterviewRequest(BaseModel):
    role: str = Field(min_length=2, max_length=80)
    persona: PersonaId

    @field_validator("role")
    @classmethod
    def normalize_role(cls, value: str) -> str:
        normalized = re.sub(r"\s+", " ", value).strip()
        if any(ord(character) < 32 for character in normalized):
            raise ValueError("Role contains unsupported control characters")
        return normalized


class GeneratedQuestion(BaseModel):
    question: str = Field(min_length=12, max_length=240)
    gesture: str = Field(min_length=2, max_length=32)


class GeneratedQuestionSet(BaseModel):
    questions: list[GeneratedQuestion] = Field(min_length=12, max_length=12)


class QuestionItem(BaseModel):
    id: int
    question: str
    tone: PersonaId
    gesture: str
    audio_file: str


class LiveInterviewManifest(BaseModel):
    persona: PersonaId
    persona_name: str
    total_questions: int = 12
    role: str
    source: Literal["openrouter"] = "openrouter"
    model: str
    session_id: str
    questions: list[QuestionItem]


class HealthResponse(BaseModel):
    status: Literal["ok"] = "ok"
    openrouter_configured: bool
    model: str
