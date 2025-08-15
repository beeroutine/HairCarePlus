#!/usr/bin/env bash
# HairCare+ full-stack launcher for local QA/dev.
# Starts Server (ASP.NET Core), Clinic (Mac Catalyst by default or iOS Simulator), Patient (physical iOS)
# Assumes: .NET SDK 9 (MAUI workload) + Xcode; physical iPhone connected & trusted for Patient.
# Usage: ./dev/run-haircare+.sh
set -euo pipefail

# If invoked from a non-bash shell (e.g., zsh), re-exec under bash to ensure bash semantics
if [[ -z "${BASH_VERSION:-}" ]]; then
  exec /bin/bash "$0" "$@"
fi

# Resolve repo root even if script is invoked via symlink.
SOURCE_PATH="${BASH_SOURCE[0]:-$0}"
ROOT_DIR="$(cd "$(dirname "$SOURCE_PATH")/.." && pwd)"
LOG_DIR="$ROOT_DIR/logs"
mkdir -p "$LOG_DIR"

PORT="${HC_PORT:-5281}"

detect_mac_ip() {
  local ip
  ip=$(ipconfig getifaddr en0 2>/dev/null || true)
  if [[ -z "$ip" ]]; then
    ip=$(ipconfig getifaddr en1 2>/dev/null || true)
  fi
  echo "$ip"
}

MAC_IP="${MAC_IP:-}"
if [[ -z "$MAC_IP" ]]; then
  MAC_IP=$(detect_mac_ip)
fi
if [[ -z "$MAC_IP" ]]; then
  echo "❌ Cannot detect host IP via 'ipconfig getifaddr en0|en1'. Set MAC_IP env var and rerun." >&2
  exit 1
fi

# Shared base URL for both MAUI clients (can be overridden from environment)
CHAT_BASE_URL="${CHAT_BASE_URL:-http://$MAC_IP:$PORT}"
export CHAT_BASE_URL

# ----------------------------------------------------------------------------
# Configuration: Clinic target (always iOS Simulator, iPhone 16 Pro preferred)
# - Set CLINIC_SIM_UDID to override simulator choice
# ----------------------------------------------------------------------------
CLINIC_SIM_UDID="${CLINIC_SIM_UDID:-}"

if [[ -z "$CLINIC_SIM_UDID" ]]; then
  echo "ℹ️  Selecting default iOS Simulator for Clinic (preferring iPhone 16 Pro → iPhone 16) ..."
  # Use JSON output from simctl to robustly pick an available iPhone simulator
  CLINIC_SIM_UDID=$(python3 - <<'PY'
import json, subprocess
prefer = ["iPhone 16 Pro", "iPhone 16"]
try:
    out = subprocess.check_output(["xcrun", "simctl", "list", "devices", "--json"], stderr=subprocess.DEVNULL)
    data = json.loads(out)
    devices = []
    for runtime, arr in data.get("devices", {}).items():
        for d in arr:
            # We only want iOS simulators that are available
            if d.get("isAvailable") and "iPhone" in d.get("name", ""):
                devices.append(d)
    # Prefer already booted
    booted = [d for d in devices if d.get("state") == "Booted"]
    ordered = booted + [d for d in devices if d.get("state") != "Booted"]
    # Prefer named models
    for name in prefer:
        for d in ordered:
            if d.get("name") == name:
                print(d.get("udid", ""))
                raise SystemExit
    # Fallback: first available iPhone
    if ordered:
        print(ordered[0].get("udid", ""))
        raise SystemExit
except Exception:
    pass
print("")
PY
  )
  if [[ -z "$CLINIC_SIM_UDID" ]]; then
    echo "⚠️  Could not resolve an iPhone simulator UDID. Open any iPhone simulator or pass CLINIC_SIM_UDID explicitly."
  fi
fi

# ----------------------------------------------------------------------------
# Ensure old server instance is not holding the port
# ----------------------------------------------------------------------------
OLD_PIDS="$(lsof -ti tcp:$PORT || true)"
if [[ -n "$OLD_PIDS" ]]; then
  echo "ℹ️  Killing existing process(es) on port $PORT: $OLD_PIDS"
  kill -9 $OLD_PIDS || true
fi

###############################################################################
# 1) API Server — must start first so MAUI clients can connect
###############################################################################
if [[ "${HC_RESET_SERVER:-0}" == "1" ]]; then
  echo "🗑  HC_RESET_SERVER=1 → wiping server state (haircareplus.db + uploads/*)"
  rm -f "$ROOT_DIR/src/Server/HairCarePlus.Server.API/haircareplus.db" 2>/dev/null || true
  rm -rf "$ROOT_DIR/src/Server/HairCarePlus.Server.API/bin/Debug/net9.0/uploads" 2>/dev/null || true
  rm -rf "$ROOT_DIR/src/Server/HairCarePlus.Server.API/uploads" 2>/dev/null || true
fi

echo "▶ Starting API server on $CHAT_BASE_URL ..."
dotnet run \
  --project "$ROOT_DIR/src/Server/HairCarePlus.Server.API" \
  --urls "http://0.0.0.0:$PORT" 2>&1 | tee "$LOG_DIR/server.log" &
SERVER_PID=$!

echo -n "⏳ Waiting for server to bind port $PORT "
until nc -z "$MAC_IP" "$PORT" ; do
  printf '.'
  sleep 0.3
done
printf " done.\n"

echo "✔ Server is up (PID $SERVER_PID)."
open -g "http://$MAC_IP:$PORT/swagger/index.html" >/dev/null 2>&1 || true
echo "🌐 Opened Swagger at http://$MAC_IP:$PORT/swagger/index.html"

###############################################################################
# 2) (Optional) clean — disabled by default to match Rider fast inner-loop
###############################################################################
if [[ "${HC_CLEAN:-0}" == "1" ]]; then
  echo "🧹 Cleaning client build outputs (HC_CLEAN=1) ..."
  set +e
  find "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2>/dev/null
  find "$ROOT_DIR/src/Client/HairCarePlus.Client.Patient" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2>/dev/null
  dotnet clean "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" >/dev/null 2>&1 || true
  dotnet clean "$ROOT_DIR/src/Client/HairCarePlus.Client.Patient" >/dev/null 2>&1 || true
  set -e
fi

###############################################################################
# Helpers
###############################################################################
resolve_patient_app_path() {
  local default_app relocated_app found
  default_app="$ROOT_DIR/src/Client/HairCarePlus.Client.Patient/bin/Debug/net9.0-ios/ios-arm64/HairCarePlus.Client.Patient.app"
  relocated_app="$ROOT_DIR/tmp/patient_build/HairCarePlus.Client.Patient/bin/Debug/net9.0-ios/ios-arm64/HairCarePlus.Client.Patient.app"
  if [[ -d "$relocated_app" ]]; then
    echo "$relocated_app"
    return 0
  fi
  if [[ -d "$default_app" ]]; then
    echo "$default_app"
    return 0
  fi
  # Fallback to search (robust to future changes)
  found=$(find "$ROOT_DIR/tmp/patient_build" -type d -name "HairCarePlus.Client.Patient.app" -print -quit 2>/dev/null || true)
  if [[ -n "$found" ]]; then
    echo "$found"
    return 0
  fi
  echo "" # not found
}

get_sim_rid() {
  if [[ "$(sysctl -in hw.optional.arm64 2>/dev/null)" == "1" ]]; then
    echo "iossimulator-arm64"
  else
    echo "iossimulator-x64"
  fi
}

###############################################################################
# 3) Patient — Rider-style run: dotnet build -t:Run (no explicit mlaunch)
###############################################################################
IPHONE_UDID="${PATIENT_UDID:-}"
if [[ -z "$IPHONE_UDID" ]]; then
  IPHONE_UDID=$(/usr/bin/python3 - <<'PY'
import json, subprocess
try:
    out = subprocess.check_output(["xcrun", "devicectl", "list", "devices", "--json-output", "/dev/stdout"], stderr=subprocess.DEVNULL)
    data = json.loads(out)
    for d in data.get("result", {}).get("devices", []):
        if d.get("deviceType", "").lower() == "physical" and d.get("platform", "").lower() == "ios" and "iphone" in d.get("name", "").lower():
            udid = d.get("udid")
            if udid:
                print(udid)
                raise SystemExit
except Exception:
    pass
print("")
PY
  )
fi
if [[ -z "$IPHONE_UDID" ]]; then
  IPHONE_UDID=$(xcrun xctrace list devices 2>/dev/null \
    | grep -vi Simulator \
    | grep -iE "iPhone|iPad" \
    | sed -n 's/.*(\([A-Fa-f0-9-]\{25,40\}\)).*/\1/p' \
    | head -n 1 || true)
fi

if [[ -z "$IPHONE_UDID" ]]; then
  echo "❌ No physical iPhone detected. Connect your iPhone (trust in Xcode) or set PATIENT_UDID."
  exit 1
fi

PATIENT_TARGET="${PATIENT_DEVICENAME:-$IPHONE_UDID}"
echo "🔧 Prebuilding Patient app (Debug, net9.0-ios, ios-arm64) ..."
dotnet build -c Debug \
  "$ROOT_DIR/src/Client/HairCarePlus.Client.Patient" \
  -f net9.0-ios \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CHAT_BASE_URL=$CHAT_BASE_URL 2>&1 | tee "$LOG_DIR/patient.prebuild.log"

echo "▶ Running Patient (Rider-style) on device=$PATIENT_TARGET ..."
dotnet build -t:Run -c Debug \
  "$ROOT_DIR/src/Client/HairCarePlus.Client.Patient" \
  -f net9.0-ios \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:_DeviceName=$PATIENT_TARGET \
  -p:MtouchLink=None -p:PublishAot=false \
  -p:NoRestore=true \
  -p:CHAT_BASE_URL=$CHAT_BASE_URL 2>&1 | tee "$LOG_DIR/patient.log" &
PATIENT_PID=$!

###############################################################################
# 4) Clinic — always run on iOS Simulator (iPhone 16 Pro preferred)
###############################################################################
if [[ -n "$CLINIC_SIM_UDID" ]]; then
  echo "▶ Booting simulator $CLINIC_SIM_UDID ..."
  xcrun simctl boot "$CLINIC_SIM_UDID" >/dev/null 2>&1 || true
  xcrun simctl bootstatus "$CLINIC_SIM_UDID" -b >/dev/null 2>&1 || true
  open -a Simulator --args -CurrentDeviceUDID "$CLINIC_SIM_UDID" >/dev/null 2>&1 || open -a Simulator >/dev/null 2>&1 || true

  SIM_RID="$(get_sim_rid)"
  echo "▶ Launching Clinic on iOS simulator (UDID $CLINIC_SIM_UDID, RID $SIM_RID) ..."
  set +e
  dotnet restore "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" -f net9.0-ios -p:RuntimeIdentifier=$SIM_RID >/dev/null 2>&1 || true
  set -e
  CLINIC_TARGET="${CLINIC_DEVICENAME:-:v2:udid=$CLINIC_SIM_UDID}"
  echo "🔧 Prebuilding Clinic app (Debug, net9.0-ios, $SIM_RID) ..."
  dotnet build -c Debug \
    "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" \
    -f net9.0-ios \
    -p:RuntimeIdentifier=$SIM_RID \
    -p:CHAT_BASE_URL=$CHAT_BASE_URL 2>&1 | tee "$LOG_DIR/clinic.prebuild.log"

  dotnet build -t:Run -c Debug -p:MtouchLink=None -p:PublishAot=false \
    "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" \
    -f net9.0-ios \
    -p:RuntimeIdentifier=$SIM_RID \
    -p:_DeviceName=$CLINIC_TARGET \
    -p:NoRestore=true \
    -p:CHAT_BASE_URL=$CHAT_BASE_URL 2>&1 | tee "$LOG_DIR/clinic.log" &
  CLINIC_PID=$!
else
  echo "⚠️  Clinic launch skipped: no simulator UDID resolved."
fi

echo "🎉 All processes started. Press Ctrl+C to stop."

trap 'echo "\n⏹ Stopping all processes..."; kill $SERVER_PID ${CLINIC_PID:-} ${PATIENT_PID:-} 2>/dev/null || true; wait' SIGINT SIGTERM

wait