# InterviewPrep — Tech Debt

**How to read:** grouped by area, sorted by severity within each area. Each item lists the file(s), the
problem, why it matters, and a rough effort (`XS` < 1h, `S` 1–4h, `M` 1–2d, `L` 3–5d).
Add items as they are discovered; remove them when fixed (with the commit).

## Severity quick index

| Sev | Count | Top items |
|---|---|---|
| Critical | 0 | — |
| High | 0 | — |
| Medium | 0 | — |
| Low | 1 | Monaco wrapper version unverified against npm |

## 1. Content pipeline

_1.1 (schema validation runner) closed by feature 001: `scripts/validate-content.sh` runs the same
validator as the importer (`import-content --validate-only`)._

## 2. Tooling

### 2.1 [LOW] Monaco wrapper choice unverified
- **Files:** `memory-bank/techContext.md`
- **Problem:** `ngx-monaco-editor-v2` version came from search snippets (npm returned 403 to the
  researcher). Decide between the wrapper and loading `monaco-editor` directly in a `@defer` block
  during feature 004.
- **Effort:** XS (decide at 004 planning)
