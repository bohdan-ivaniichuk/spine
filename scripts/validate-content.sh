#!/usr/bin/env bash
# Validate content/**/*.json against the committed schemas and import rules. No database needed.
# Usage: scripts/validate-content.sh [--content-dir <path>]
set -euo pipefail
cd "$(dirname "$0")/.."
exec dotnet run --project src/Api -c Release --no-launch-profile -- import-content --validate-only "$@"
