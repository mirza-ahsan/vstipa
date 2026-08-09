import json
import os
import sys
import yaml
from pathlib import Path
from typing import List
from pydantic import BaseModel, Field
from dotenv import load_dotenv
from google import genai
from google.genai import types

load_dotenv()

class QuestionItem(BaseModel):
    question: str = Field(..., description="The spoken interview question text")
    tone: str = Field(..., description="The tone tag of the question (warm, stern, neutral)")
    gesture: str = Field(..., description="Gesture tag for avatar animation")

class QuestionList(BaseModel):
    questions: List[QuestionItem] = Field(..., description="List of 10 to 14 interview questions")

def load_persona(persona_path: Path) -> dict:
    with open(persona_path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)

def generate_questions_for_persona(persona: dict, client: genai.Client) -> QuestionList:
    persona_id = persona["persona_id"]
    system_prompt = persona["system_prompt"]
    prompt = f"{system_prompt}\n\nPlease output JSON conforming to the QuestionList schema."

    config = types.GenerateContentConfig(
        response_mime_type="application/json",
        response_schema=QuestionList,
        thinking_config=types.ThinkingConfig(thinking_level="high")
    )

    for attempt in range(2):
        try:
            print(f"[{persona_id}] Generating questions (attempt {attempt + 1})...")
            response = client.models.generate_content(
                model="gemini-3.6-flash",
                contents=prompt,
                config=config,
            )
            data = json.loads(response.text)
            validated = QuestionList.model_validate(data)
            if 10 <= len(validated.questions) <= 14:
                return validated
            print(f"[{persona_id}] Question count {len(validated.questions)} outside 10-14 range, retrying...")
        except Exception as e:
            print(f"[{persona_id}] Attempt {attempt + 1} failed: {e}")
            if attempt == 1:
                raise e

    raise RuntimeError(f"[{persona_id}] Failed to generate valid QuestionList after retries.")

def main():
    backend_dir = Path(__file__).parent
    personas_dir = backend_dir / "personas"
    output_dir = backend_dir / "output"
    streaming_dir = backend_dir.parent / "unity-project" / "Assets" / "StreamingAssets" / "questions"

    output_dir.mkdir(parents=True, exist_ok=True)

    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        print("GEMINI_API_KEY environment variable not set in .env. Cannot run live generation.")
        sys.exit(1)

    client = genai.Client(api_key=api_key)

    for persona_file in personas_dir.glob("*.yaml"):
        persona = load_persona(persona_file)
        persona_id = persona["persona_id"]
        q_list = generate_questions_for_persona(persona, client)

        out_path = output_dir / f"questions_{persona_id}.json"
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(q_list.model_dump_json(indent=2))
        print(f"Saved: {out_path}")

        persona_streaming_dir = streaming_dir / persona_id
        persona_streaming_dir.mkdir(parents=True, exist_ok=True)

if __name__ == "__main__":
    main()
