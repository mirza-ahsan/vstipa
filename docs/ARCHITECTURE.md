# V-STIPA Architecture Document

## Overview
V-STIPA (Virtual Synthetic Trainer for Interview Performance & Analysis) is a VR training-simulation demo designed for Quest headsets.

## Key Design Principles
1. **Bake, Don't Call (Zero On-Stage Network Dependency)**
   - Question content and voice audio are generated ahead of time on the dev machine (Stage A).
   - Generated assets (`manifest.json` + audio files) are baked into `unity-project/Assets/StreamingAssets/questions/`.
   - On stage runtime (Stage B) runs 100% offline in Quest airplane mode.
2. **Reliability First**
   - No live speech-to-text or microphone capture.
   - User answers questions out loud to the physical room.
   - Single button advances through pre-rendered questions with synchronized lip-sync and gestures.

## Two-Stage Pipeline
- **Stage A (Dev Machine, Pre-Demo)**
  - `backend/generate_questions.py`: Calls Gemini 3.6 Flash (`thinking_level: "high"`) with persona system prompts. Returns structured JSON containing array of `{question, tone, gesture}`.
  - `backend/render_audio.py`: Takes question text and calls Google Cloud Text-to-Speech (WaveNet/Chirp3-HD) to generate audio clips and builds `manifest.json`.
- **Stage B (Quest Headset, Runtime)**
  - Reads `StreamingAssets/questions/<persona>/manifest.json` and audio files.
  - Controls Ready Player Me humanoid avatar animations (Mixamo gestures) and lip-sync via uLipSync.

## Optional Stretch Feature (Phase 6)
- USB-C tether with `adb reverse tcp:8000 tcp:8000`.
- Lightweight FastAPI endpoint with strict 3-second timeout, falling back silently to pre-baked content on network failure.
