#!/usr/bin/env bash
# Stop — if the session touched >= 5 source files but never updated
# memory-bank/activeContext.md, nudge the user.

INPUT=$(cat)
TRANSCRIPT=$(echo "$INPUT" | jq -r '.transcript_path // ""')
[[ -z "$TRANSCRIPT" || ! -f "$TRANSCRIPT" ]] && exit 0

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
threshold=5

# Count edits inside the project, excluding memory-bank, working/ and specs/ (spec work is tracked by Spec Kit).
edits=$(grep -oE "\"file_path\":\"${ROOT}[^\"]*\"" "$TRANSCRIPT" 2>/dev/null \
  | grep -vE '/(memory-bank|working|specs|\.specify)/' | wc -l | tr -d ' ')
active=$(grep -oE "\"file_path\":\"${ROOT}/memory-bank/(activeContext|progress)\.md\"" "$TRANSCRIPT" 2>/dev/null | wc -l | tr -d ' ')

if (( edits >= threshold && active == 0 )); then
  msg="Memory-bank staleness check: ${edits} file edits this session, but memory-bank/activeContext.md / progress.md were not updated. Consider updating them to reflect this session's work."
  jq -n --arg m "$msg" '{systemMessage:$m, suppressOutput:true}'
fi
exit 0
