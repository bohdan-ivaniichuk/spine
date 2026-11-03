#!/usr/bin/env bash
# PreToolUse Bash — force an explicit confirmation before any `git push`,
# REGARDLESS of permission mode (including auto mode / -p headless).
#
# Local commits run without a prompt (user decision, 2026-11-03): they are cheap to amend or
# reset and the Lockflight red/green flow makes many of them. Publishing is what needs the OK.
#
# A PreToolUse hook that returns permissionDecision "ask" overrides the normal permission
# flow, so even auto mode cannot silently push. In non-interactive runs with no user to
# confirm, the push is aborted — which is the intended safe default.
#
# Read-only git and local writes (status, log, diff, add, commit, branch, …) are untouched.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
[[ "$TOOL" != "Bash" ]] && exit 0

CMD=$(echo "$INPUT" | jq -r '.tool_input.command // ""')

# Normalize newlines to spaces so chained commands are scanned on one line.
CMD_NORM=" $(printf '%s' "$CMD" | tr '\n' ' ') "

# Match `git [global flags] push` where only git option flags may sit between `git` and the
# subcommand (so `git log … push` / `git diff -- push.md` do NOT match).
# Leading boundary covers start, command separators (; & | &&) and plain spaces.
if printf '%s' "$CMD_NORM" | grep -Eq '(^|[;&|]| )git( +-[A-Za-z-]+( +[^ ]+)?)* +push([ ;&|]|$)'; then
  cat <<JSON
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "ask",
    "permissionDecisionReason": "git push requires explicit confirmation in this workspace — never push without the user's OK, even in auto mode."
  }
}
JSON
  exit 0
fi

exit 0
