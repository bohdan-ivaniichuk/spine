#!/usr/bin/env bash
# PreToolUse Write — refuse to overwrite docker-compose.override.yml (local-only, secrets).
# Edits are allowed; only full Writes are blocked.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
FILE=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""')

[[ "$TOOL" != "Write" ]] && exit 0

if [[ "$FILE" == *"docker-compose.override.yml" ]]; then
  MSG="Blocked: Write to docker-compose.override.yml. Needs your approval."
  "$(dirname "$0")/notify.sh" "override-yml-protect" "$MSG" >/dev/null 2>&1 &
  echo "Blocked by override-yml-protect: docker-compose.override.yml is a gitignored local-only file holding secrets. Refusing to overwrite via Write. Use Edit, or ask the user to confirm the overwrite explicitly." >&2
  exit 2
fi
exit 0
