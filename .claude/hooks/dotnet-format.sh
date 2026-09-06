#!/usr/bin/env bash
# PostToolUse Edit/Write — run `dotnet format whitespace` on the edited .cs file
# in the background so the chat is not blocked. Silent on success and failure.

INPUT=$(cat)
TOOL=$(echo "$INPUT" | jq -r '.tool_name // ""')
FILE=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""')

case "$TOOL" in
  Edit|Write|MultiEdit) ;;
  *) exit 0 ;;
esac

[[ "$FILE" != *.cs ]] && exit 0
[[ ! -f "$FILE" ]] && exit 0

# Walk up from the file to find the containing .csproj.
dir=$(dirname "$FILE")
proj=""
while [[ "$dir" != "/" && "$dir" != "." ]]; do
  found=$(find "$dir" -maxdepth 1 -name "*.csproj" 2>/dev/null | head -1)
  if [[ -n "$found" ]]; then proj="$found"; break; fi
  dir=$(dirname "$dir")
done

[[ -z "$proj" ]] && exit 0

( dotnet format whitespace "$proj" --include "$FILE" --no-restore --verbosity quiet >/dev/null 2>&1 ) &
disown
exit 0
