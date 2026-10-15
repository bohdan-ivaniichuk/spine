#!/usr/bin/env bash
# SessionStart — load the "live" memory-bank files (activeContext, progress,
# systemPatterns). The static files (projectbrief, productContext, techContext)
# are available on demand via Read.

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
files=(activeContext.md progress.md systemPatterns.md)

echo "Spine memory-bank (live files only — activeContext, progress, systemPatterns). Static files (projectbrief, productContext, techContext) are available on demand via Read."
echo

for f in "${files[@]}"; do
  p="$ROOT/memory-bank/$f"
  if [[ -f "$p" ]]; then
    echo "===== memory-bank/$f ====="
    cat "$p"
    echo
  fi
done
exit 0
