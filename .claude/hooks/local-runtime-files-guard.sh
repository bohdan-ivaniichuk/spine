#!/usr/bin/env bash
# PreToolUse Edit/Write/MultiEdit — block writes to gitignored local-only files that
# hold real secrets. `*.example` variants have a different basename and pass through.
#   Blocked: .env, appsettings.Local.json
#   docker-compose.override.yml is handled separately by override-yml-protect.sh.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
FILE=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""')

case "$TOOL" in
  Edit|Write|MultiEdit) ;;
  *) exit 0 ;;
esac

block() {
  local rule="$1" detail="$2"
  MSG="Blocked by local-runtime-files-guard [$rule]: $(basename "$FILE"). Needs your approval."
  "$(dirname "$0")/notify.sh" "local-runtime-files-guard" "$MSG" >/dev/null 2>&1 &
  echo "Blocked by local-runtime-files-guard [$rule]: $detail" >&2
  exit 2
}

case "$(basename "$FILE")" in
  ".env")
    block "env-file" \
      ".env is a gitignored local-only file holding real secrets (ANTHROPIC_API_KEY, DB password). \
Edit it manually outside AI. Use .env.example as the tracked template." ;;
  "appsettings.Local.json")
    block "local-config" \
      "appsettings.Local.json is a gitignored local-only file holding real secrets. \
Edit it manually outside AI. Use appsettings.Local.json.example as the tracked template." ;;
esac

exit 0
