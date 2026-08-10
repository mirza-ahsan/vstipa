# V-STIPA — Virtual Synthetic Trainer for Interview Performance & Analysis

**V-STIPA** (Virtual Synthetic Trainer for Interview Performance & Analysis) is a VR
training-simulation demo built for a single 3–4 minute live presentation. A user
puts on a Meta Quest headset, faces a virtual interviewer avatar, and presses a button
to advance through a set of AI-generated interview questions — delivered with
real-time lip-sync and gestures. The on-stage build runs **fully offline**; all
question content and voice audio are AI-generated ahead of time and baked into the app.

> Scope note: this is a demo build, not a production/multi-user app. Live conversation
> memory, microphone capture, and answer processing are intentionally out of scope.

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
| 3 | Avatar & lip-sync (3 personas) | ✅ Done | ✅ | Meta Horizon-style 3D avatar with grey blazer outfit, beanie, facial viseme blendshapes (viseme_aa/E/O/U/smile), gesture animator, and real-time FFT lip-sync deployed & verified on Quest |
| 4 | Staging, environment, performance polish | ⬜ Not started | ⬜ | |
| 5 | Full dry-run rehearsal | ⬜ Not started | ⬜ | |
| 6 | *(Optional)* Live-mode stretch feature | ⬜ Not started | ⬜ | Only attempt if Phase 5 is solid with time to spare |

**Status legend:** ⬜ Not started · 🟨 In progress · ✅ Done · ⚠️ Blocked

**What's left right now:** everything — this tracker should be updated as each phase
closes. Once Phase 5 is ✅, the app is demo-ready; Phase 6 is a bonus, not a requirement.

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
uv run render_audio.py           # calls Cloud TTS, writes audio + manifest
```
Copy the output into `unity-project/Assets/StreamingAssets/questions/` if the script
doesn't already write there directly.

### 8. Build & deploy
Standard Unity Android build targeting the connected Quest (File → Build Settings →
Android → Build and Run), or `adb install -r <path-to-apk>` for an existing build.

---

## Verifying the Demo Is Actually Reliable

Before trusting this on stage, run the Phase 2 gate check yourself: **put the headset
in airplane mode** and go through the full question sequence for at least one persona,
start to finish. If it works with the radio off, it will work in a room full of
other people's Wi-Fi and Bluetooth traffic. Do this again as part of Phase 5's dry-run
rehearsal, on battery power, timed with a stopwatch, at least three times in a row
without a glitch, before considering the build demo-ready.

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
