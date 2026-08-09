# V-STIPA Phase Gates Specification

Each phase has strict completion criteria. A phase cannot be marked done in `README.md` or committed until all gate criteria are verified.

## Phase 0 — Environment & Toolchain
- **Verification:**
  - Python 3.12.13 verified via `uv run python --version`.
  - Android platform tools (`adb`) installed and responsive (`adb devices`).
  - Unity Hub installed at `/usr/bin/unityhub`.
  - Directory structure scaffolded per `masterprompt.md` §7.
  - `mcp_config.json` configured.
  - Git identity confirmed (`user.name`, `user.email`).

## Phase 1 — Content Generation Pipeline (Stage A)
- **Verification:**
  - `generate_questions.py` outputs valid JSON matching Pydantic schema for all 3 personas (warm, stern, neutral).
  - `render_audio.py` renders audio clips via Google Cloud TTS and updates `manifest.json`.
  - Output populated into `unity-project/Assets/StreamingAssets/questions/`.
  - Backend unit tests pass (`uv run pytest`).

## Phase 2 — Offline Playback Core Loop
- **Verification:**
  - Quest headset in **airplane mode** (Wi-Fi off).
  - Unity application reads baked manifest and advances through audio clips on button press.

## Phase 3 — Avatar & Lip-Sync
- **Verification:**
  - 3 Ready Player Me avatars imported with Humanoid rig.
  - uLipSync drives lip movement in sync with audio.
  - Mixamo gesture triggers accurately according to question metadata.

## Phase 4 — Staging, Polish, Performance
- **Verification:**
  - Environment staging complete.
  - Sustained target frame rate on Quest headset (verified via OVR Metrics Tool).

## Phase 5 — Full Dry-Run Rehearsal
- **Verification:**
  - 3 consecutive clean runs of the full 3–4 minute demo on battery power in airplane mode.

## Phase 6 — Live-Mode Stretch Feature (Optional)
- **Verification:**
  - USB tethered live API call succeeds within 3s.
  - Pulling tether cable mid-request silently falls back to offline baked question.
