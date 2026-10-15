---
name: sync-claude-md
description: >-
  Reconcile the curated CLAUDE.md against the actual codebase and apply surgical fixes — verify every
  factual claim (project/test names, code symbols, file & doc paths, ports, scripts, hooks), report
  drift, then patch ONLY what is wrong while preserving structure, ordering, and gotchas. Use instead
  of /init to MAINTAIN an existing CLAUDE.md. Triggers: "sync CLAUDE.md", "check/update CLAUDE.md
  against the code", "fix CLAUDE.md drift", "is CLAUDE.md still accurate".
argument-hint: "[report | <repo-name>]   (no arg = audit + propose fixes; report = dry-run, no edits)"
allowed-tools: >-
  Read, Grep, Glob, Edit,
  Bash(grep *), Bash(find *), Bash(ls *),
  Bash(git -C * ls-files*), Bash(git -C * status*)
---

# Sync CLAUDE.md (reconcile, don't regenerate)

This is the **maintenance** counterpart to `/init`. `/init` regenerates a CLAUDE.md from scratch and
should run **once** on a repo that has none. This skill instead **verifies the existing curated
CLAUDE.md against reality and patches only the drift** — it never rewrites prose, never reorders
sections, never strips gotchas or forward-looking notes.

If no CLAUDE.md exists at the canonical location, stop and tell the user to run `/init` instead — this
skill does not bootstrap.

## Canonical location

The single source of truth is the **repo-root** `CLAUDE.md` of Spine (the session's project
root). There are intentionally no per-folder CLAUDE.md files under `src/` or `web/`. Always reconcile
that file.

## Modes (`$ARGUMENTS`)

| Argument | Mode |
|---|---|
| _(empty)_ | Full audit → drift report → **propose** fixes → apply after confirmation. |
| `report` | Dry-run: audit + drift report only, make **no** edits. |
| `<area>` (`api`, `web`, `content`, `docker`) | Limit verification to claims about that area. |

## Procedure

### 1. Read CLAUDE.md and extract verifiable claims

Read it whole. Pull out the **checkable** assertions (skip opinions/narrative) into these buckets:

- **Structure** — projects under `src/` and `tests/`, the `.sln` name, `web/` app name, `content/` layout.
- **Code symbols** — named classes / interfaces / endpoints / DbContexts / enum-literals
  (e.g. `ISchedulingAlgorithm`, `Sm2Algorithm`, `AppDbContext`, `ImportContentCommand`).
- **Paths** — referenced docs (`docs/**/*.md`, `docs/architecture/STACK.md`, `TECH_DEBT.md`,
  `memory-bank/*.md`), `.claude/skills/*`, `.claude/agents/*`, `.specify/**`, `specs/**`, `content/**`.
- **Infra** — docker-compose service names, ports.
- **Hooks** — every hook named in CLAUDE.md vs the files in `.claude/hooks/` and their wiring in `.claude/settings.json`.
- **Commands** — build/test/EF/migration invocations that name real projects or contexts.

### 2. Verify each claim with read-only tools

Use Grep/Glob/Bash to confirm existence. Patterns that work for this workspace:

```bash
# projects / tests
find src tests -maxdepth 2 -name '*.csproj'
ls web/src/app

# a named code symbol actually exists (count > 0)
grep -rl 'interface ISchedulingAlgorithm' --include='*.cs' src
grep -rhoE 'class [A-Za-z]+DbContext' --include='*.cs' src | sort -u

# referenced files exist
ls <path-from-claude-md>

# docker services / ports
grep -nE '^  [a-z0-9-]+:' docker-compose.yml
grep -nE '8080|4200|5432' docker-compose.yml

# hooks / agents present vs referenced
ls .claude/hooks/ .claude/agents/
```

### 3. Classify every finding

| Class | Meaning | Action |
|---|---|---|
| ✅ Accurate | claim matches code | none |
| ❌ Stale/wrong | path renamed, symbol gone, project renamed | **surgical fix** to the exact token |
| ⚠️ Forward-looking | documented but not yet in code (e.g. a planned BackgroundService) | **keep**, add a short `(planned — not yet implemented)` qualifier; never delete |
| ➕ Undocumented | a real, load-bearing structure/gotcha missing from the doc | propose adding **only** if it would bite someone; do not add standard/obvious facts |

### 4. Present the drift report

A compact table: claim → reality → class → proposed edit. In `report` mode, stop here.

### 5. Apply fixes (after explicit confirmation)

- Edit **surgically** — change only the wrong token/sentence. Preserve indentation, ordering,
  surrounding prose, tables, and all gotchas.
- **Propagate the same fact-fix to other files that repeat it** — a stale path or symbol often also
  lives in a hook comment / error message under `.claude/hooks/` or in a skill under `.claude/skills/`. Grep for the
  old value workspace-wide and fix every functional occurrence (skip dead entries in
  `settings.local.json` — those are harmless leftovers, leave them or flag for `/update-config`).
- Never restructure the document. Never regenerate. If a section is genuinely large/wrong, propose the
  rewrite to the user rather than doing it silently.

### 6. Report

List exactly what changed (file + token), what was left as-is (forward-looking items), and any
undocumented gotchas you chose **not** to add. Do not claim "in sync" unless every ❌ was resolved.

## Guardrails

- **Reconcile, never regenerate.** This skill must not produce a from-scratch CLAUDE.md.
- **Preserve curation.** Gotchas, the content pipeline description, and narrative stay byte-for-byte
  unless a token in them is factually wrong.
- **Don't bloat.** Apply the CLAUDE.md exclusion principle — leave out anything a senior engineer would
  already infer from the code.
- **Confirm before writing** (except in `report` mode). Editing a hook's comment is fine; editing hook
  *logic* is out of scope — flag it instead.
- Authored to the Claude Code standard under `.claude/`.
