# Phase 5 Recording Handoff

## Automated evidence completed

- Unity WebGL build succeeded at `local-build/webgl/` (88,690,881 bytes reported by Unity).
- Local server responds at <http://127.0.0.1:8000/> and serves PCM WAV assets.
- Unity EditMode tests: 3 passed, 0 failed.
- Backend tests: 4 passed, 0 failed.
- Phase 3/4 validator passed room/UI, three persona configurations, 36 questions,
  audio references, gestures, uLipSync, and the static-face fallback.
- A clean Chromium flow selected Warm, advanced all 12 questions, reached completion,
  returned to the menu, and selected Stern with no page, request, Unity exception, or
  audio-sample errors.
- Runtime logs confirmed 24 kHz PCM playback with real durations and uLipSync MFCC
  phoneme analysis for both tested personas.
- Visual evidence is in `artifacts/phase5/`.

## Honest gate limitations

- Only one local Avaturn source GLB exists. It is reused for three persona presets.
- That GLB has no facial blendshapes. The current mouth animation is an explicit visual
  proxy; it is not the final real-viseme facial rig.
- The current GLB's native generic idle clip is used. Its runtime Humanoid conversion
  was verified separately but cannot be combined safely with that generic clip, so
  automatic conversion is disabled for the T1 fallback.
- `adb devices` currently lists no headset. No Quest performance or footage claim can
  be made from this session.
- A headless Chromium software renderer measured poorly and is not a valid hardware
  performance result. Profile the actual browser/recording machine, or Quest if used.

## Human recording checklist

1. Open <http://127.0.0.1:8000/> and confirm sound is audible before recording.
2. Record a Warm rehearsal with natural spoken-answer pauses; target 3–4 minutes.
3. Record Stern and Neutral rehearsals, checking question readability and flow.
4. Review all three recordings for audio balance, mouth visibility, animation, cursor,
   notifications, and dropped frames.
5. Capture the final submission using the strongest persona/run.
6. Rewatch the exported video from start to finish before submission.

If three distinct facial-rig Avaturn exports arrive, replace the shared fallback,
verify Humanoid mapping, retarget the gesture clips, map uLipSync to the real facial
targets, rebuild, and repeat this checklist.
