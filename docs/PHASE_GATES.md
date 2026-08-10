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
  - Unity reads baked manifests and advances through the PCM clips on button press.
  - The baked path remains a reliable fallback. Airplane-mode verification is useful
    resilience evidence, but is no longer a recorded-submission requirement.

## Phase 3 — Avatar & Lip-Sync
- **Verification:**
  - 3 distinct Avaturn GLB exports are imported with valid Humanoid mappings.
  - uLipSync drives actual facial blendshapes in sync with readable PCM audio.
  - Retargeted gesture clips trigger accurately according to question metadata.
  - The warm, stern, and neutral personas each pass a full local flow.
  - A separate review pass verifies the gate. A static-face mouth proxy is acceptable
    for the Track 1 fallback, but does not satisfy the final real-viseme criterion.

## Phase 4 — Staging, Polish, Performance
- **Verification:**
  - Interview-room staging, persona selection, question HUD, and intentional start/end
    states are complete.
  - A clean WebGL rebuild passes the full browser interaction flow without runtime errors.
  - Sustained frame rate is measured on the actual recording target. Quest/OVR Metrics
    verification is required only if headset footage is used.

## Phase 5 — Full Dry-Run Rehearsal
- **Verification:**
  - 3 consecutive human-paced 3–4 minute recorded rehearsals complete without a flow break.
  - Audio, avatar motion, readable UI, and capture quality are reviewed in each recording.
  - The final recorded submission is captured and checked end-to-end.

## Phase 6 — Live-Mode Stretch Feature (Optional)
- **Verification:**
  - USB tethered live API call succeeds within 3s.
  - Pulling tether cable mid-request silently falls back to offline baked question.
