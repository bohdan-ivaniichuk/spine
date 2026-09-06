#!/usr/bin/env bash
# PreToolUse Bash — force an explicit confirmation before any `git commit` or `git push`,
# REGARDLESS of permission mode (including auto mode / -p headless).
#
# A PreToolUse hook that returns permissionDecision "ask" overrides the normal permission
# flow, so even auto mode cannot silently commit or push. In non-interactive runs with no
# user to confirm, the action is aborted — which is the intended safe default.
#
# Read-only git (status, log, diff, show, fetch, branch, add, restore, …) is untouched.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
[[ "$TOOL" != "Bash" ]] && exit 0

CMD=$(echo "$INPUT" | jq -r '.tool_input.command // ""')

# Normalize newlines to spaces so chained commands are scanned on one line.
CMD_NORM=" $(printf '%s' "$CMD" | tr '\n' ' ') "

# Match `git [global flags] commit|push` where only git option flags may sit between
# `git` and the subcommand (so `git log … commit` / `git diff -- commit.md` do NOT match).
# Leading boundary covers start, command separators (; & | &&) and plain spaces.
if printf '%s' "$CMD_NORM" | grep -Eq '(^|[;&|]| )git( +-[A-Za-z-]+( +[^ ]+)?)* +(commit|push)([ ;&|]|$)'; then
  SUB=$(printf '%s' "$CMD_NORM" | grep -Eo 'git( +-[A-Za-z-]+( +[^ ]+)?)* +(commit|push)' | grep -Eo '(commit|push)' | head -1)
  cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "ask",
    "permissionDecisionReason": "git ${SUB} requires explicit confirmation in this workspace — never commit or push without the user's OK, even in auto mode."
  }
}
EOF
  exit 0
fi

exit 0
