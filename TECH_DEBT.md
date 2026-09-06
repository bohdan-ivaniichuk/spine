# InterviewPrep — Tech Debt

**How to read:** grouped by area, sorted by severity within each area. Each item lists the file(s), the
problem, why it matters, and a rough effort (`XS` < 1h, `S` 1–4h, `M` 1–2d, `L` 3–5d).
Add items as they are discovered; remove them when fixed (with the commit).

## Severity quick index

| Sev | Count | Top items |
|---|---|---|
| Critical | 0 | — |
| High | 0 | — |
| Medium | 1 | Content validation relies on `npx ajv-cli` until the API `import-content` validator exists |
| Low | 1 | Monaco wrapper version unverified against npm |

## 1. Content pipeline

### 1.1 [MEDIUM] Schema validation has no project-owned runner yet
- **Files:** `content/schema/content.schema.json`, `.claude/agents/content-author.md`
- **Problem:** Until feature 001 ships `import-content`, validation runs through `npx -y ajv-cli@5`,
  which is not pinned in the repo and needs network on first run.
- **Why it matters:** `content-author` may claim "valid" against a slightly different validator than the
  importer will use later.
- **Fix:** Feature 001 adds the importer; then add a `scripts/validate-content.sh` that calls it (or a
  tiny Node script with the same `ajv` version pinned in `web/package.json`).
- **Effort:** S

## 2. Tooling

### 2.1 [LOW] Monaco wrapper choice unverified
- **Files:** `memory-bank/techContext.md`
- **Problem:** `ngx-monaco-editor-v2` version came from search snippets (npm returned 403 to the
  researcher). Decide between the wrapper and loading `monaco-editor` directly in a `@defer` block
  during feature 004.
- **Effort:** XS (decide at 004 planning)
