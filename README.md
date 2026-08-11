# V-STIPA — Virtual Synthetic Trainer for Interview Performance & Analysis

**V-STIPA** (Virtual Synthetic Trainer for Interview Performance & Analysis) is a VR
training-simulation demo built for a recorded submission. A user faces a virtual
interviewer avatar and presses a button to advance through a set of AI-generated
interview questions — delivered with lip-sync and gestures. The reliable Track 1
fallback keeps all question content and PCM voice audio baked into the app; offline
operation is useful but is no longer a submission requirement.

> Scope note: this is a demo build, not a production/multi-user app. It can generate a
> fresh role-based question set at interview start, but live conversation memory,
> microphone capture, answer scoring, and follow-up generation remain out of scope.

---

## Progress Tracker

Update this table at the end of every phase. Each phase has an independent gate check
(see `docs/PHASE_GATES.md`) — don't check a phase off until its gate has actually been
verified by a separate review pass, not just "it seemed to work."

| Phase | Description | Status | Gate Verified | Notes |
|---|---|---|---|---|
| 0 | Environment & toolchain setup | ✅ Done | ✅ | Verified uv Python 3.12.13, adb, Unity Hub, MCP config & repo scaffold |
| 1 | Content generation pipeline (Gemini → questions → TTS audio) | ✅ Done | ✅ | Pipeline scripts, persona definitions, schemas, unit tests, and baked manifest+audio assets created for warm, stern, neutral personas |
| 2 | Offline playback core loop (airplane-mode test) | ✅ Done | ✅ | QuestionPlaybackController, PlaybackUI, WorldSpace Canvas, and NUnit tests built; 25MB APK deployed & running on Quest in airplane mode |
| 3 | Avatar & lip-sync (3 personas) | 🟨 In progress | ⬜ | Verified T1 Avaturn body, native idle/procedural gestures, uLipSync MFCC input, and a static-face mouth proxy run in WebGL. Only one avatar source is available and it has no facial morphs; three distinct facial-rig exports and real-viseme wiring remain |
| 4 | Staging, environment, performance polish | 🟨 In progress | ⬜ | Interview room, persona-selection UI, question HUD, completion/restart flow, and persona accents are built and browser-tested; Quest performance validation remains hardware-blocked |
| 5 | Full dry-run rehearsal | 🟨 In progress | ⬜ | Automated local WebGL flow reaches completion and switches personas; three human-paced recorded rehearsals and final submission recording remain |
| 6 | Role-based live interview generation | 🟨 In progress | ⬜ | Target-role UI, secure local OpenRouter backend, strict 12-question schema, lazy male TTS, and baked fallback are implemented; live verification awaits a rotated API key |

**Status legend:** ⬜ Not started · 🟨 In progress · ✅ Done · ⚠️ Blocked

**What's left right now:** supply three suitable facial-rig avatar exports, complete an
independent Phase 3 review, run Quest checks only if headset footage is required, and
record three clean human-paced rehearsals plus the final submission. The role-based
mode also needs one live pass after a fresh OpenRouter key is configured locally.

---

## Hardware Requirements

| Item | Requirement | Notes |
|---|---|---|
| VR headset | Meta Quest 2 / 3 / Pro | Standalone rendering — the headset's own chip does all rendering, not the PC |
| Dev machine | Any x86-64 machine capable of running Unity 6 (tested on a Dell Precision 5520, 7th-gen i7) | Only runs the Unity Editor and the content-generation scripts — never in the render loop |
| Cable | USB-C (headset ↔ dev machine) | Needed for deploy/debug during development, and for the optional live-mode stretch feature. **Not needed for the on-stage demo itself** — the shipped build is fully offline. |
| Network | None required for the demo | Only the one-time content-generation step (Phase 1) needs internet, on the dev machine, before the demo |

## Software Requirements

| Tool | Version | Purpose |
|---|---|---|
| Unity | 6 LTS | Game engine, Android/Quest build target |
| Android Build Support, SDK, NDK, OpenJDK | via Unity Hub | Quest APK builds |
| Python | 3.12.13 (via `uv`) | Content-generation scripts |
| `uv` | latest | Python environment/dependency management |
| `adb` | latest (Android platform-tools) | Deploy, logs, port forwarding — replaces Meta Quest Developer Hub, which is **Windows/macOS only and not available on Linux** |
| Meta XR All-in-One SDK, XR Interaction Toolkit, XR Plugin Management, uLipSync, Newtonsoft Json | via Unity Package Manager | See `masterprompt.md` §6.4 |
| Antigravity CLI | latest | Agentic build tool used to implement this plan phase-by-phase, with Unity MCP + Meta XR Unity MCP Extension connected for direct Editor access |
| Google AI Studio API key | — | Gemini 3.6 Flash calls (content generation only, dev-machine only, never shipped in the build) |
| Google Cloud project with Billing + Text-to-Speech API enabled | — | Voice rendering for the baked question audio (stays within the free tier at this project's volume) |
| OpenRouter API key | — | Optional live role-based question generation; held only by the local backend and never shipped to Unity/WebGL |

---

## Setup — Any OS

The steps below are OS-general; distro/OS-specific package manager commands are called
out where they differ. Full detail and rationale for each choice lives in
`docs/ARCHITECTURE.md`.

### 1. Python environment
```bash
curl -LsSf https://astral.sh/uv/install.sh | sh      # macOS/Linux
# Windows: powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

uv python install 3.12.13
cd backend
uv venv --python 3.12.13
source .venv/bin/activate        # Windows: .venv\Scripts\activate
uv pip install -r requirements.txt   # or: uv sync, if using pyproject.toml
```

### 2. Android/Quest toolchain
- Install `adb`:
  - **Arch/CachyOS:** `yay -S android-tools`
  - **Debian/Ubuntu:** `sudo apt install android-tools-adb`
  - **macOS:** `brew install android-platform-tools`
  - **Windows:** install via Android Studio's SDK Platform-Tools, or standalone
    platform-tools zip from Google
- Enable Developer Mode on the Quest headset via the Meta Horizon mobile app.
- Connect via USB-C, run `adb devices`, accept the in-headset "Allow USB debugging"
  prompt.
- **Windows/macOS only:** Meta Quest Developer Hub (MQDH) is available and gives a GUI
  for the above plus device management — optional convenience layer, not required.
- **Linux:** MQDH is not available; the `adb`-based workflow above is the full
  equivalent. Install OVR Metrics Tool via `adb install <path-to-apk>` for the
  on-headset performance HUD MQDH would otherwise provide.

### 3. Unity
- Install Unity Hub (platform-appropriate installer/package manager).
- Through Hub, install **Unity 6 LTS** with modules: Android Build Support, Android
  SDK & NDK Tools, OpenJDK.
- Open `unity-project/` in the Hub once cloned.

### 4. Unity packages
Open Package Manager inside the Unity Editor and add, in order: Meta XR All-in-One
SDK, XR Interaction Toolkit, XR Plugin Management (enable OpenXR + Meta Quest feature
group under Project Settings), uLipSync (via Git URL), Newtonsoft Json.

### 5. Credentials (never committed)
Copy `backend/.env.example` to `backend/.env` and fill in:
```
GEMINI_API_KEY=...
GOOGLE_APPLICATION_CREDENTIALS=path/to/cloud-tts-service-account.json
OPENROUTER_API_KEY=...
OPENROUTER_MODEL=openrouter/free
```

### 6. Antigravity CLI + MCP
Install Antigravity CLI and connect it to: a Unity MCP server (official Unity AI open
beta MCP Server, for direct Editor access — scene hierarchy, console, tests, builds),
the Meta XR Unity MCP Extension (Quest-specific scene/interaction tooling), and a
filesystem MCP scoped only to this project directory. See `docs/ARCHITECTURE.md` for
the full setup steps.

### 7. Generate the baked content (Phase 1)
```bash
cd backend
uv run generate_questions.py     # calls Gemini, writes questions_<persona>.json
uv run render_audio.py           # calls Cloud TTS, writes PCM WAV + manifest
```
Copy the output into `unity-project/Assets/StreamingAssets/questions/` if the script
doesn't already write there directly.

`generate_offline_assets.py` uses persona-matched male Edge neural voices and also
needs `ffmpeg` on `PATH` to convert the generated audio to 24 kHz mono PCM WAV files
that WebGL and uLipSync can sample reliably. This remains a generation-stage tool;
the deployed Unity application only loads the baked clips.

### 8. Build & deploy
For the recorded browser fallback, use **V-STIPA → Build WebGL** in Unity, then serve
`local-build/webgl/` from the repository root:

```bash
python -m http.server 8000 --directory local-build/webgl
```

Open <http://127.0.0.1:8000/>. For a Quest build, use **V-STIPA → Build Android**.
The build command configures OpenXR for Android, generates the tracked seated camera
and headset UI, validates the Quest settings, and writes:

```text
local-build/android/V-STIPA-Quest.apk
```

Wake and connect one authorized Quest, then install and launch it with:

```bash
./scripts/deploy-quest.sh
```

Quest controls are intentionally deterministic: **A/right trigger** selects Warm or
advances, **B** selects Neutral or returns to the persona menu, and **X/Y** selects
Stern. The APK contains the room, avatar, question manifests, and PCM voice audio;
no separate environment package or runtime server is required.

### 9. Run role-based live mode

Keep the static WebGL server on port 8000 and start the local API in a second terminal:

```bash
cd backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Open <http://127.0.0.1:8000/>, enter a target position, and choose a persona. Unity
posts only the role and persona to the local API. The API key remains in
`backend/.env`; it is never embedded in the build or returned to the browser. If live
generation fails, the app automatically continues with the baked 12-question set.

---

## Verifying the Demo Is Actually Reliable

Before recording the final submission, go through the full question sequence for each
persona and record at least three human-paced rehearsals. The baked Track 1 path should
remain the recovery baseline. If Quest footage is required, repeat the checks on the
headset, on battery power, and use airplane mode as an optional resilience test.

---

## Project Structure

```
vstipa/
├── backend/              # content-generation scripts (Gemini + Cloud TTS), run once pre-demo
├── unity-project/        # Quest app; StreamingAssets/questions holds the baked audio+manifest
├── docs/
│   ├── ARCHITECTURE.md   # system design, setup detail, rationale for each tool choice
│   ├── TESTING.md        # test strategy and manual QA checklist
│   └── PHASE_GATES.md    # what "done" means for each phase in the tracker above
└── README.md
```

## Full Build Plan

`docs/ARCHITECTURE.md` and `docs/PHASE_GATES.md` are the source of truth for
architecture decisions, phase-by-phase scope, and what's deliberately out of scope.
This README tracks *status*; those docs define *what "done" means* for each phase.
