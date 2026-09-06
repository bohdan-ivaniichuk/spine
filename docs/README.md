# docs/

Stable, lookup-style documentation for InterviewPrep. `CLAUDE.md` is the operational source of truth
and links here; `memory-bank/` holds the living state; `specs/` holds feature designs.

| Path | What's in it |
|---|---|
| `architecture/STACK.md` | Stable technical reference — versions, ports, folder/namespace conventions, verified dates and sources. |
| `architecture/decisions/` | Architecture Decision Records (ADRs) — one numbered file per significant decision, newest appended. |

Related, but **not** under `docs/`:

- `../.specify/` + `../specs/` — spec-driven development via [Spec Kit](https://github.com/github/spec-kit).
  `.specify/memory/constitution.md` is the binding principles document; `specs/000-taxonomy.md` is the
  content scope; `specs/NNN-feature/` holds each feature's `spec.md` / `plan.md` / `tasks.md`.
- `../content/` — seed content and its JSON Schema.
- `../memory-bank/` — living project state (loaded at session start).
- `../working/` — personal scratch, git-ignored.
- `../TECH_DEBT.md` — prioritised debt list.

## Adding an ADR

Copy the format of `architecture/decisions/0001-workspace-shell-and-spec-kit.md`: next number, short
title, `Status` + `Date`, then `Context` / `Decision` / `Consequences`. ADRs are immutable once Accepted —
to reverse one, write a new ADR that supersedes it.
