# Active Context — InterviewPrep

## Where things stand (2026-09-06, branch `dev`)

Workspace shell bootstrapped from the MailHub shell (generic parts only) and Spec Kit re-initialised
with the current CLI. No application code yet.

**Done this session**
- `.specify/` (fresh Spec Kit), constitution 1.0.0 in place, taxonomy at `specs/000-taxonomy.md`.
- `.claude/`: hooks (secrets-scan, local-runtime-files-guard, override-yml-protect,
  git-commit-push-confirm, dotnet-format, web-format, load-memory-banks, memory-bank-staleness, notify),
  agents (code-reviewer, frontend-reviewer, content-author, debugger, solution-architect, test-author,
  deep-researcher), skills (speckit-*, sync-claude-md, official angular + dotnet skills), `settings.json`.
- `.mcp.json`: context7, playwright, postgres (read-only), angular-cli (read-only).
- `content/schema/content.schema.json` per the kickoff spec.
- `CLAUDE.md`, `memory-bank/`, `docs/`, `TECH_DEBT.md`, `.gitignore`, `working/`.

**Next**
1. `/speckit-constitution` validation pass (no regeneration).
2. `/speckit-specify` feature 001 Foundation → `/speckit-clarify` (data-model questions only, for the user).
3. In parallel: `content-author` on `dotnet/clr-memory` → review digest for the user.
4. After clarify: `solution-architect` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`.

**Waiting on the user**
- Answers to clarify questions that change the data model (feature 001).
- Content review of `content/dotnet/clr-memory.json` (flip `reviewed: true`).
