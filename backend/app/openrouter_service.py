import asyncio
import json
import os

import httpx

from .schemas import GeneratedQuestionSet


OPENROUTER_ENDPOINT = "https://openrouter.ai/api/v1/chat/completions"
PERSONA_GUIDANCE = {
    "warm": "Supportive and encouraging, while still asking substantive follow-up questions.",
    "neutral": "Balanced, concise, and professional, with measured technical depth.",
    "stern": "Direct and challenging, emphasizing trade-offs, ownership, and production judgment.",
}
PERSONA_GESTURES = {
    "warm": ["nod", "lean_forward", "smile", "idle", "thinking"],
    "neutral": ["nod", "idle", "thinking", "lean_forward"],
    "stern": ["arms_crossed", "lean_back", "nod_firm", "idle", "thinking"],
}


class OpenRouterUnavailable(RuntimeError):
    pass


class OpenRouterService:
    def __init__(self, api_key: str | None = None, model: str | None = None):
        self.api_key = api_key or os.getenv("OPENROUTER_API_KEY", "")
        self.model = model or os.getenv("OPENROUTER_MODEL", "openrouter/free")

    @property
    def configured(self) -> bool:
        return bool(self.api_key)

    async def generate_questions(self, role: str, persona: str) -> tuple[GeneratedQuestionSet, str]:
        if not self.configured:
            raise OpenRouterUnavailable("OPENROUTER_API_KEY is not configured")

        gestures = PERSONA_GESTURES[persona]
        schema = {
            "name": "role_interview_questions",
            "strict": True,
            "schema": {
                "type": "object",
                "properties": {
                    "questions": {
                        "type": "array",
                        "minItems": 12,
                        "maxItems": 12,
                        "items": {
                            "type": "object",
                            "properties": {
                                "question": {"type": "string", "minLength": 12, "maxLength": 240},
                                "gesture": {"type": "string", "enum": gestures},
                            },
                            "required": ["question", "gesture"],
                            "additionalProperties": False,
                        },
                    }
                },
                "required": ["questions"],
                "additionalProperties": False,
            },
        }
        system_prompt = (
            "You create realistic interview-preparation questions. Return exactly 12 "
            "distinct questions "
            "for the requested job role: a deliberate mix of role-specific technical, behavioral, "
            "situational, collaboration, and problem-solving questions. Do not ask for "
            "protected personal "
            "information. Ask questions only; never provide answers or commentary. "
            f"Interviewer style: {PERSONA_GUIDANCE[persona]}"
        )
        payload = {
            "model": self.model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": f"Target position: {role}"},
            ],
            "response_format": {"type": "json_schema", "json_schema": schema},
            "temperature": 0.65,
            "max_tokens": 1800,
            "stream": False,
        }
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
            "HTTP-Referer": "http://127.0.0.1:8000",
            "X-OpenRouter-Title": "V-STIPA Interview Simulator",
        }

        async with httpx.AsyncClient(timeout=httpx.Timeout(30.0, connect=8.0)) as client:
            for attempt in range(2):
                try:
                    response = await client.post(OPENROUTER_ENDPOINT, headers=headers, json=payload)
                except httpx.HTTPError as exc:
                    raise OpenRouterUnavailable("Could not reach OpenRouter") from exc
                if response.status_code in (429, 502, 503, 504) and attempt == 0:
                    try:
                        retry_after = float(response.headers.get("Retry-After", "1"))
                    except ValueError:
                        retry_after = 1.0
                    await asyncio.sleep(min(retry_after, 3.0))
                    continue
                if response.is_error:
                    raise OpenRouterUnavailable(f"OpenRouter returned HTTP {response.status_code}")

                try:
                    envelope = response.json()
                    content = envelope["choices"][0]["message"]["content"]
                    parsed = GeneratedQuestionSet.model_validate(json.loads(content))
                except (KeyError, IndexError, TypeError, json.JSONDecodeError, ValueError) as exc:
                    raise OpenRouterUnavailable(
                        "OpenRouter returned an invalid question set"
                    ) from exc

                normalized = {item.question.casefold().strip(" ?.!") for item in parsed.questions}
                if len(normalized) != 12:
                    raise OpenRouterUnavailable("OpenRouter returned duplicate questions")
                return parsed, str(envelope.get("model") or self.model)

        raise OpenRouterUnavailable("OpenRouter did not return a usable response")
