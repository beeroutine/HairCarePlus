#!/usr/bin/env bash
# Starts Server API + Clinic MAUI + Patient MAUI concurrently.
# Requires: Android Emulator + iOS Simulator (or change target frameworks below).
# Usage: ./scripts/run-stack.sh [clinic-framework] [patient-framework]
# Example: ./scripts/run-stack.sh net9.0-ios net9.0-android
set -euo pipefail

CLINIC_FRAMEWORK=${1:-net9.0-ios}
PATIENT_FRAMEWORK=${2:-net9.0-android}

SERVER_PROJECT="src/Server/HairCarePlus.Server.API/HairCarePlus.Server.API.csproj"
CLINIC_PROJECT="src/Client/HairCarePlus.Client.Clinic/HairCarePlus.Client.Clinic.csproj"
PATIENT_PROJECT="src/Client/HairCarePlus.Client.Patient/HairCarePlus.Client.Patient.csproj"

# Function to clean up background jobs on exit
cleanup() {
  echo "\nStopping all background processes..."
  jobs -p | xargs -r kill
}
trap cleanup EXIT

echo "Starting Server API (dotnet watch)..."
DOTNET_WATCH="dotnet watch -q -p $SERVER_PROJECT run"
$DOTNET_WATCH &
SERVER_PID=$!

# Give Kestrel a few seconds to boot
sleep 3

echo "Launching Clinic app ($CLINIC_FRAMEWORK)..."
dotnet build "$CLINIC_PROJECT" -t:Run -f "$CLINIC_FRAMEWORK" &
CLINIC_PID=$!

echo "Launching Patient app ($PATIENT_FRAMEWORK)..."
dotnet build "$PATIENT_PROJECT" -t:Run -f "$PATIENT_FRAMEWORK" &
PATIENT_PID=$!

echo "All processes started. Press Ctrl+C to stop."
wait 