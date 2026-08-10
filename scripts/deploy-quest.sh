#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
apk_path="${1:-${repo_root}/local-build/android/V-STIPA-Quest.apk}"
package_name="com.mirzaahsan.vstipa"
activity_name="com.unity3d.player.UnityPlayerGameActivity"

if [[ ! -f "${apk_path}" ]]; then
  echo "Quest APK not found: ${apk_path}" >&2
  echo "Build it with Unity menu V-STIPA > Build Android first." >&2
  exit 1
fi

device_count="$(adb devices | awk '$2 == "device" { count++ } END { print count + 0 }')"
if [[ "${device_count}" -ne 1 ]]; then
  echo "Expected exactly one authorized Quest, found ${device_count}." >&2
  echo "Wake and connect the headset, accept USB debugging, then retry." >&2
  adb devices -l >&2
  exit 2
fi

echo "Installing ${apk_path}..."
adb install -r "${apk_path}"
adb logcat -c
adb shell am force-stop "${package_name}"
adb shell am start -W -n "${package_name}/${activity_name}"

pid="$(adb shell pidof "${package_name}" | tr -d '\r')"
if [[ -z "${pid}" ]]; then
  echo "The package installed but no running process was found." >&2
  exit 3
fi

echo "V-STIPA is running on Quest (PID ${pid})."
echo "Runtime logs: adb logcat -s Unity ActivityManager OpenXR"
