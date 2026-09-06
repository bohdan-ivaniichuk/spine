# 1. Bootstrap the workspace from the MailHub shell and adopt Spec Kit

- **Status:** Accepted
- **Date:** 2026-09-06

## Context

InterviewPrep starts as an empty folder with a constitution and a content taxonomy. An existing
workspace shell (the MailHub multi-repo workspace) already encodes a proven Claude Code setup: blocking
hooks for secrets and local files, a commit/push confirmation gate, format-after-edit hooks, a
memory-bank loaded at session start, read-only reviewer subagents, a research subagent, a
`sync-claude-md` maintenance skill, and Spec Kit for spec-driven development.

Most of that shell is generic; the rest is MailHub-specific (BaGet feed checks, RabbitMQ and carrier
guards, validation-layer guard, order-status warnings, per-repo memory banks).

## Decision

- Copy the generic parts as-is (`override-yml-protect`, `git-commit-push-confirm`, `dotnet-format`,
  `notify`, `working/` convention, `.specify` templates), adapt the rest to a **single-repo** layout
  (`secrets-scan` gains Anthropic/OpenAI key patterns; `local-runtime-files-guard` keeps only `.env` and
  `appsettings.Local.json`; `load-memory-banks` / `memory-bank-staleness` read one `memory-bank/`;
  `code-reviewer` / `debugger` / `solution-architect` / `test-author` / `deep-researcher` are rewritten
  around the InterviewPrep constitution), and drop everything MailHub-specific.
- Add what this project needs: `frontend-reviewer` (Angular), `content-author` (seed generation from
  the taxonomy), `web-format` hook (prettier + `ng lint --fix`), the content JSON Schema, and
  project-scope MCP servers (`context7`, `playwright`, `postgres` read-only, `angular-cli` read-only).
- Re-initialise Spec Kit with the current CLI (`specify init --here --force --non-interactive
  --integration claude`) rather than copying the older MailHub copy, keeping the hand-written
  constitution as the source of truth.
- Install the official `angular/skills` and a curated subset of `dotnet/skills` project-scope
  (`skills-lock.json` pins them) instead of the larger community bundles.
- Single git repository; `dev` is the working branch, `main` receives merges.

## Consequences

- The workflow is `/speckit-specify → clarify → plan → tasks → implement` with subagent reviews before
  every commit, identical in shape to the MailHub workflow, so habits transfer.
- Hooks are enforced through `.claude/settings.json`; `git commit` / `git push` always ask.
- MailHub-specific knowledge stays out; "Things that bite" in `CLAUDE.md` starts empty and grows only
  from real incidents.
- Spec Kit skills are the upstream ones; future `specify init --force` refreshes are safe because the
  constitution lives in `.specify/memory/` and is never regenerated.
