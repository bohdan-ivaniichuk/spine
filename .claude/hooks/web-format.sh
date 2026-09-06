#!/usr/bin/env bash
# PostToolUse Edit/Write — format Angular sources under web/ in the background:
# prettier --write on the file, then `ng lint --fix` on it if the Angular CLI + ESLint
# builder are available. Silent on success and failure; never blocks the chat.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
FILE=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""')

case "$TOOL" in
  Edit|Write|MultiEdit) ;;
  *) exit 0 ;;
esac

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
WEB="$ROOT/web"

[[ "$FILE" != "$WEB/"* ]] && exit 0
[[ ! -f "$FILE" ]] && exit 0
case "$FILE" in
  *.ts|*.html|*.scss) ;;
  *) exit 0 ;;
esac
[[ ! -f "$WEB/package.json" ]] && exit 0

(
  cd "$WEB" || exit 0
  if [[ -x node_modules/.bin/prettier ]]; then
    node_modules/.bin/prettier --write --log-level silent "$FILE" >/dev/null 2>&1
  fi
  if [[ "$FILE" == *.ts || "$FILE" == *.html ]] && [[ -x node_modules/.bin/ng ]] \
     && grep -q '"lint"' package.json 2>/dev/null; then
    node_modules/.bin/ng lint --fix --lint-file-patterns "$FILE" >/dev/null 2>&1
  fi
) &
disown
exit 0
