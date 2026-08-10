# V-STIPA Testing Strategy & Manual QA Checklist

## Automated Testing
- **Backend Tests (`backend/tests/`)**
  - `test_schema.py`: Validates persona YAML definitions and output JSON schema adherence.
  - `test_generate_questions.py`: Unit test with mocked Gemini API responses.
- **Unity Tests (`unity-project/Assets/Scripts/Tests/`)**
  - Playback controller manifest parsing, step bounds, and baked PCM WAV readability tests.
- **Scene/build validator (`V-STIPA → Validate Phase 3 + 4`)**
  - Checks room/UI references, persona wiring, 36 manifest entries, referenced audio,
    gesture/lip-sync components, and explicitly reports avatar-source/blendshape counts.
- **Local WebGL smoke pass**
  - Drives persona selection, all 12 questions, completion, and persona switching in a
    clean browser context while capturing console/request errors and screenshots.

## Gate Verification Checklist
- **Phase 0 Gate:**
  - Environment verified (`uv run python --version` -> 3.12.13).
  - Toolchain verified (`adb`, `unityhub`, Antigravity CLI).
  - Repo scaffold complete with docs and configs.
- **Phase 1 Gate:**
  - End-to-end question generation and audio rendering produces valid JSON + audio clips.
  - Human review of questions & audio playback.
- **Phase 2 Gate:**
  - Cycling through a baked question set succeeds without a live backend.
  - Airplane mode is an optional resilience check for headset footage.
- **Phase 3 Gate:**
  - Three distinct Avaturn facial rigs are present and verified as Humanoid.
  - Avatar facial lip-sync matches pre-baked audio.
  - Gesture animations trigger correctly per question tone/gesture tag.
- **Phase 4 Gate:**
  - Frame budget maintained on the chosen recording target.
  - If Quest footage is used, profile it on Quest via OVR Metrics Tool.
  - Start/end states polished.
- **Phase 5 Gate:**
  - 3 consecutive recorded, human-paced 3–4 minute runs without glitches.
  - Final submission recording reviewed from start to finish.
- **Phase 6 Gate (Optional):**
  - Live mode calls work over USB-C tether; mid-call disconnection falls back silently.
