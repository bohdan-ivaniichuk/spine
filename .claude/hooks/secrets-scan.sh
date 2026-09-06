#!/usr/bin/env bash
# PreToolUse Edit/Write — refuse content that looks like a real secret.
# Conservative regexes; false positives are worse than misses here, so we only
# match strong indicators (Anthropic/OpenAI/AWS/Stripe/Slack/GitHub keys, JWTs,
# and connection strings with a literal Password=... that is not a placeholder).

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
FILE=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""')

case "$TOOL" in
  Edit|Write|MultiEdit) ;;
  *) exit 0 ;;
esac

# Skip files that are expected to contain secrets locally (all gitignored).
case "$FILE" in
  *"docker-compose.override.yml") exit 0 ;;
  *"/.env"|*"/.env."*)             exit 0 ;;
  *"appsettings.Local.json"|*"appsettings.Development.json") exit 0 ;;
esac

CONTENT=$(echo "$INPUT" | jq -r '
  .tool_input.new_string //
  .tool_input.content //
  ([.tool_input.edits[]?.new_string] | join("\n")) //
  ""
')

block() {
  MSG="Blocked: $1 in $(basename "$FILE"). Needs your approval."
  "$(dirname "$0")/notify.sh" "secrets-scan" "$MSG" >/dev/null 2>&1 &
  echo "Blocked by secrets-scan: $1. Move it to .env (gitignored), user-secrets, or docker-compose.override.yml." >&2
  exit 2
}

# Anthropic API key
echo "$CONTENT" | grep -qE 'sk-ant-[A-Za-z0-9_-]{20,}' && block "Anthropic API key (sk-ant-)"
# OpenAI-style keys
echo "$CONTENT" | grep -qE 'sk-(proj|svcacct)-[A-Za-z0-9_-]{20,}' && block "OpenAI API key"
# AWS access key (permanent + temporary)
echo "$CONTENT" | grep -qE '(AKIA|ASIA)[0-9A-Z]{16}' && block "AWS access key id"
# Stripe secret / webhook keys
echo "$CONTENT" | grep -qE 'sk_(live|test)_[A-Za-z0-9]{24,}' && block "Stripe secret key"
echo "$CONTENT" | grep -qE 'whsec_[A-Za-z0-9]{10,}' && block "Stripe webhook secret"
# Slack tokens
echo "$CONTENT" | grep -qE 'xox[baprs]-[0-9A-Za-z-]{10,}' && block "Slack token"
# GitHub PAT / fine-grained
echo "$CONTENT" | grep -qE 'gh[pousr]_[A-Za-z0-9]{30,}' && block "GitHub token"
# JWT-looking bearer (3 base64url segments, realistically long)
echo "$CONTENT" | grep -qE 'Bearer[[:space:]]+ey[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}' && block "Bearer JWT in code"
# Connection string with literal Password= (not a placeholder like ${...}, %..%, or {{...}})
if echo "$CONTENT" | grep -qiE '(Host|Server|Data Source)=[^;"]*;[^"]*Password=[A-Za-z0-9!@#$%^&*_+=-]{6,}'; then
  case "$CONTENT" in
    *'Password=${'*|*'Password=%'*|*'Password={{'*) ;;
    *) block "DB connection string with literal password" ;;
  esac
fi

exit 0
