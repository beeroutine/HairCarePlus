#!/usr/bin/env bash
# HairCare+ full-stack launcher for local QA.
# Starts Server (ASP.NET Core), Clinic (Mac Catalyst), Patient (physical iOS)
# Assumes: dotnet SDK 9 is installed and an iPhone 15 Pro with specified UDID is connected.
# Usage: ./dev/run-haircare+.sh
set -euo pipefail

# Resolve repo root even if script is invoked via symlink.
# In bash, $BASH_SOURCE[0] exists; in zsh it is undefined, so fall back to $0.
SOURCE_PATH="${BASH_SOURCE[0]:-$0}"
ROOT_DIR="$(cd "$(dirname "$SOURCE_PATH")/.." && pwd)"

# Your Mac's Wi-Fi IP address (adjust interface if your NIC is different)
MAC_IP=$(ipconfig getifaddr en0 || true)
if [[ -z "$MAC_IP" ]]; then
  echo "❌ Cannot detect IP via 'ipconfig getifaddr en0'. Edit dev/run-haircare+.sh and set MAC_IP manually." >&2
  exit 1
fi

# Shared chat base URL env var for both MAUI clients
export CHAT_BASE_URL="http://$MAC_IP:5281"

# -----------------------------------------------------------------------------
# Select which build of Clinic to run
# If caller didn't export CLINIC_SIM_UDID, fall back to default iPhone-16 simulator
# -----------------------------------------------------------------------------
DEFAULT_SIM_UDID="A6B34CF4-02AE-4304-8D44-7D0AF1F007DF"
CLINIC_SIM_UDID="${CLINIC_SIM_UDID:-$DEFAULT_SIM_UDID}"

# -----------------------------------------------------------------------------
# Ensure old server instance is not holding port 5281 (macOS uses BSD xargs w/o -r)
# -----------------------------------------------------------------------------
OLD_PIDS="$(lsof -ti tcp:5281 || true)"
if [[ -n "$OLD_PIDS" ]]; then
  echo "ℹ️  Killing existing process(es) on port 5281: $OLD_PIDS"
  kill -9 $OLD_PIDS || true
fi

###############################################################################
# 1) API Server — must start first so MAUI clients can connect
###############################################################################
echo "▶ Starting API server on $CHAT_BASE_URL ..."
dotnet run \
  --project "$ROOT_DIR/src/Server/HairCarePlus.Server.API" \
  --urls "http://0.0.0.0:5281" &
SERVER_PID=$!

# Wait until port 5281 is accepting connections
echo -n "⏳ Waiting for server to bind port 5281 "
until nc -z "$MAC_IP" 5281 ; do
  printf '.'
  sleep 0.3
done
printf " done.\n"

echo "✔ Server is up (PID $SERVER_PID)."

###############################################################################
# 2) Clinic app
###############################################################################
if [[ -n "${CLINIC_SIM_UDID:-}" ]]; then
  echo "▶ Booting simulator $CLINIC_SIM_UDID ..."
  xcrun simctl boot $CLINIC_SIM_UDID >/dev/null 2>&1 || true

  # Choose correct simulator RID based on host CPU
  HOST_ARCH="$(uname -m)"
  if [[ "$HOST_ARCH" == "arm64" ]]; then
    SIM_RID="iossimulator-arm64"
  else
    SIM_RID="iossimulator-x64"
  fi

  echo "▶ Launching Clinic on iOS simulator (UDID $CLINIC_SIM_UDID, RID $SIM_RID) ..."
  dotnet build -t:Run \
    "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" \
    -f net9.0-ios \
    -p:RuntimeIdentifier=$SIM_RID \
    -p:_DeviceName=:v2:udid=$CLINIC_SIM_UDID &
  CLINIC_PID=$!
else
  echo "▶ Launching Clinic (Mac Catalyst) ..."
  dotnet build -t:Run \
    "$ROOT_DIR/src/Client/HairCarePlus.Client.Clinic" \
    -f net9.0-maccatalyst &
  CLINIC_PID=$!
fi

###############################################################################
# 3) Patient — physical iPhone 15 Pro over USB/Wi-Fi
###############################################################################
IPHONE_UDID="00008130-000444D83891401C"

echo "▶ Deploying Patient app to iPhone ($IPHONE_UDID) ..."
dotnet build -t:Run \
  "$ROOT_DIR/src/Client/HairCarePlus.Client.Patient" \
  -f net9.0-ios \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:_DeviceName=$IPHONE_UDID &
PATIENT_PID=$!

echo "🎉 All processes started. Press Ctrl+C to stop."

# Forward SIGINT/SIGTERM to children and clean up properly
trap 'echo "\n⏹ Stopping all processes..."; kill $SERVER_PID $CLINIC_PID $PATIENT_PID 2>/dev/null || true; wait' SIGINT SIGTERM

wait 