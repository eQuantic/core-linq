#!/usr/bin/env bash
# semantic-release verifyConditions step: refuse to start a release without the NuGet
# key — BEFORE anything is analyzed, tagged, committed or published.
set -euo pipefail

if [ -z "${NUGET_API_KEY:-}" ]; then
  {
    echo "NUGET_API_KEY is empty — releases are disarmed."
    echo "Create the 'nuget' GitHub environment (Settings → Environments) with a"
    echo "NUGET_API_KEY secret (and optionally required reviewers for a manual"
    echo "approval gate), then re-run the workflow."
  } >&2
  exit 1
fi
