# Contract — `import-content` CLI verb

Invocation:

```bash
dotnet run --project src/Api -- import-content [--content-dir <path>] [--validate-only] [--dry-run]
docker compose run --rm api import-content            # CONTENT_DIR=/content (./content mounted read-only)
scripts/validate-content.sh                            # = --validate-only, no database needed
```

`content-dir` resolution: `--content-dir` → `CONTENT_DIR` env → `./content`.

## Phases

1. **Discover** topic files `<content-dir>/*/*.json` (excluding `schema/`). None and no manifest →
   prints `nothing to import`, exit 0.
2. **Manifest** — validate `manifest.json` against `schema/manifest.schema.json`; rule
   `manifest-duplicates` (unique domain slugs, unique topic slugs per domain).
3. **Schema validation** of every topic file against `schema/content.schema.json` (draft 2020-12);
   every error collected as `(file, jsonPointer, rule=keyword, message)`; all files processed.
4. **Rule validation**: `manifest-pair`, `file-path` (`<domain>/<topic>.json`), `duplicate-topic-file`,
   `concept-slug-unique`, `id-prefix` (`<domain>.<topic>.<concept>.`), `id-type` (type equals the id
   segment), `mcq-one-correct` (per language), `concept-title` (equals `translations.en.title`),
   `duplicate-id` (across files; names both files).
   `--validate-only` stops here.
5. **Plan** — build the import plan with content hashes; derive `concepts.reviewed`.
6. **Diff** (pure) — creates / updates / unchanged / deprecations / reactivations per entity type and
   per file; translations diffed per language; existing question with different `type` → error.
7. **Apply** — one transaction; `pg_try_advisory_xact_lock(7411001)` (false → exit 2); tracked graph
   loaded; change set applied; `SaveChanges`; commit (`--dry-run` → rollback, summary still printed).
   Progress tables are never touched.
8. **Report** — per-file table and totals of created / updated / unchanged / deprecated for domains,
   topics, concepts, questions, katas, translations.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | success, or nothing to import |
| 1 | validation or usage errors (manifest, schema, rules, type change, unknown argument, missing schema files) — nothing written |
| 2 | import already in progress (advisory lock held) |
| 3 | database / unexpected failure, content directory not found, cancelled |

## Error line format

```
<file>:<jsonPointer> [<rule>] <message>
content/dotnet/clr-memory.json:/concepts/2/questions/0/translations/en/options [maxItems] must NOT have more than 4 items
content/dotnet/clr-memory.json:/concepts/2/questions/1 [mcq-one-correct] exactly one option must be correct (found 2)
```
