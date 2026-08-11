# V-STIPA Architecture Document

## Overview
V-STIPA (Virtual Synthetic Trainer for Interview Performance & Analysis) is a VR training-simulation demo designed for Quest headsets.

## Key Design Principles
1. **Reliable baked baseline with an optional live path**
   - Question content and voice audio are generated ahead of time on the dev machine (Stage A).
   - Generated assets (`manifest.json` + audio files) are baked into `unity-project/Assets/StreamingAssets/questions/`.
   - Runtime can request role-specific questions from the local backend, but every
     network, credential, model, validation, or TTS failure returns to the baked path.
2. **Reliability First**
   - No live speech-to-text or microphone capture.
   - User answers questions out loud to the physical room.
   - Single button advances through pre-rendered questions with synchronized lip-sync and gestures.

## Two-Stage Pipeline
- **Stage A (Dev Machine, Pre-Demo)**
  - `backend/generate_questions.py`: Calls Gemini 3.6 Flash (`thinking_level: "high"`) with persona system prompts. Returns structured JSON containing array of `{question, tone, gesture}`.
  - `backend/render_audio.py`: Takes question text and calls Google Cloud Text-to-Speech (WaveNet/Chirp3-HD) to generate audio clips with 3.5x digital gain PCM amplification and builds `manifest.json`.
- **Stage B (Quest Headset, Runtime)**
  - Reads `StreamingAssets/questions/<persona>/manifest.json` and audio files.
  - Controls 3D Humanoid Interviewer Avatars (`male_avatar.glb`, `female_avatar.glb` baked in `unity-project/Assets/Avatars/`).
  - Real-time FFT spectrum viseme lip-sync (`AvatarLipSync.cs`) and parametric gestures (`AvatarGestureController.cs` for nod, lean, arms crossed, etc.).

## 3D Avatar & Lip-Sync Decision (Final Architecture)
- **Avatar Models**: 3D Humanoid business attire avatars (`male_avatar.glb` for Male interviewer, `female_avatar.glb` for Female interviewer).
- **Offline Deployment**: Avatars are stored locally in `Assets/Avatars/` and compiled inside the standalone APK. Zero runtime network or external CDN requests.
- **Lip-Sync**: Real-time 256-sample FFT audio spectrum formant analyzer (`AvatarLipSync.cs`) driving viseme blendshapes (`viseme_aa`, `viseme_O`, `viseme_E`, `viseme_U`) and RMS volume fallback.

## Role-Based Live Mode (Phase 6)
- The user enters a target role before selecting the interviewer persona.
- Unity posts `{role, persona}` to FastAPI on port 8001; no API credential is present
  in the Unity scene, WebGL files, or Quest APK.
- FastAPI calls OpenRouter's chat-completions endpoint with a strict JSON schema and
  accepts only 12 distinct, validated questions.
- Male question audio is generated lazily and cached per interview session as 24 kHz
  mono PCM WAV for the existing uLipSync playback path.
- Any live failure automatically loads the existing `StreamingAssets` manifest.
- For a tethered Quest, reverse both ports: `adb reverse tcp:8000 tcp:8000` and
  `adb reverse tcp:8001 tcp:8001`.
