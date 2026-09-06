#!/usr/bin/env bash
# Shared notifier. Two modes:
#
#   1) Called by a blocking hook: pass title + message as args.
#        notify.sh "rabbitmq-guard" "blocked RabbitMQ in mailhub-app-api"
#
#   2) Called from the Notification hook event: stdin is the hook JSON
#      (contains a `message` field — typically a permission prompt or
#      idle-wait notice). No args needed.
#
# Uses macOS osascript for a native banner with a sound. Silent on Linux.

if ! command -v osascript >/dev/null 2>&1; then
  exit 0
fi

if [[ $# -ge 1 ]]; then
  TITLE="Claude Code — ${1:-hook}"
  MSG="${2:-blocked}"
else
  INPUT=$(cat)
  TITLE="Claude Code"
  MSG=$(echo "$INPUT" | jq -r '.message // "needs your input"')
fi

# Escape double quotes for AppleScript.
TITLE_ESC=${TITLE//\"/\\\"}
MSG_ESC=${MSG//\"/\\\"}

osascript -e "display notification \"${MSG_ESC}\" with title \"${TITLE_ESC}\" sound name \"Glass\"" >/dev/null 2>&1 || true
exit 0
