# V-STIPA Testing Strategy & Manual QA Checklist

## Automated Testing
- **Backend Tests (`backend/tests/`)**
  - `test_schema.py`: Validates persona YAML definitions and output JSON schema adherence.
  - `test_generate_questions.py`: Unit test with mocked Gemini API responses.
- **Unity Tests (`unity-project/Assets/Scripts/Tests/`)**
  - Playback controller manifest parsing & step logic unit tests.

## Gate Verification Checklist
- **Phase 0 Gate:**
  - Environment verified (`uv run python --version` -> 3.12.13).
  - Toolchain verified (`adb`, `unityhub`, Antigravity CLI).
  - Repo scaffold complete with docs and configs.
- **Phase 1 Gate:**
  - End-to-end question generation and audio rendering produces valid JSON + audio clips.
  - Human review of questions & audio playback.
- **Phase 2 Gate:**
  - On-headset run in airplane mode (Wi-Fi completely off).
  - Cycling through full question set succeeds offline.
- **Phase 3 Gate:**
  - Avatar lip-sync matches pre-baked audio.
  - Gesture animations trigger correctly per question tone/gesture tag.
- **Phase 4 Gate:**
  - Frame budget maintained on Quest (profiled via OVR Metrics Tool).
  - Start/end states polished.
- **Phase 5 Gate:**
  - 3 consecutive 3-4 minute full dry runs on battery power in airplane mode without glitches.
- **Phase 6 Gate (Optional):**
  - Live mode calls work over USB-C tether; mid-call disconnection falls back silently.
