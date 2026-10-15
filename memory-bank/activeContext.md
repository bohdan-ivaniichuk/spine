# Active Context — Spine

## Where things stand (2026-10-15, branch `dev`)

Features **001 Foundation**, **002 Review session** and **003 Atlas redesign + EN/UK UI + rename**
are complete. The product is now **Spine — the senior engineer's interview atlas** (formerly
InterviewPrep); solution `Spine.slnx`, projects/namespaces `Spine.*`, compose project `spine`
(postgres volume pinned to `interviewprep_postgres_data`, DB/role still `interviewprep`).
Tests: 237 .NET (20 domain, 76 application, 77 API, 64 infrastructure) + 80 web, all green; both
reviewers clean after fixes; walked in Chrome in EN/UK and light/dark.

**003 in short (ADR 0006)**
- Atlas look: light ground, ink panels, domain colors 01–05, Unbounded + Golos Text + JetBrains Mono;
  dark Atlas variant; every token pair ≥ 4.5:1.
- Shell: B1 "spine" rail (sections, domain tabs → `/review?scope=domain&domain=…`, EN/УК, theme, API).
- Runtime i18n: `web/src/app/shared/i18n` (signal `lang`, typed dictionaries, Intl plurals,
  `withLang` adds `?lang=uk` only when not English). Store re-reads the open session on a switch.
- API: `?lang=en|uk` on all review endpoints (per-question fallback via `TranslationPicker`,
  `language`/`fallback` fields), new `GET /api/review/coverage` for the atlas tiles.
- Overview: hero, ink Today card, atlas with per-topic coverage; the scope lives in the URL.

**2026-10-15 (later) — after 003**
- `dotnet/clr-memory` and `dotnet/async` reviewed by the user → `reviewed: true`, plus full `uk` pass.
- Ukrainian domain/topic titles: manifest `titles.uk` (all 5 domains, 20 topics), nullable `title_uk`
  columns (migration `AddDomainTopicUkTitles`), `LocalizedTitle.Pick` on every title read, tree carries
  `titleFallback` (review refs/scopes carry no flag — deliberate).
- Content sprint 1 (EN, `reviewed: false`): `csharp-lang`, `dotnet-platform`, all of `aspnet-data`, all of
  `architecture` — 409 questions, 5 katas. Digest + author-flagged facts in
  `working/content-review-digest-sprint1.md`.
- Api.Tests import a frozen fixture (`tests/Fixtures/api-content`) with the real schemas, so content
  growth no longer breaks them; `RepoContentValidatesTests` still guards the real content.
- **004 Knowledge atlas & notes done** (ADR 0007): `/atlas` → domain → topic → concept, Markdown notes
  (markdown-it `html:false` + highlight.js core, lazy concept chunk ≈ 60 kB gz), EN badge/`lang` on fallback,
  per-level document titles, focus to `#main` on navigation. Readiness = every schedulable (MCQ) question
  has `interval_days >= 7` (ease unused — SM-2 never raises it), served by `GET /api/review/readiness`
  (4 constant queries, `ReadinessCalculator` floor %, null when nothing counts). New session scope `concept`
  (`sessions.scope_concept_slug`, migration `AddConceptSessionScope`); shared `ReviewQueryables.Eligible`
  for sessions and readiness. Badges moved to `web/src/app/shared/badges/`.
- Tests: 322 .NET + 158 web, gate `--profile pr` PASS. Lockflight trace covers FR-008/009/010/012 only —
  web-only FRs are verified by Vitest (TECH_DEBT 3.3).

**Next**
1. User: remove the stale test baseline (`.lockflight/baselines/known-failing-tests.json`, entry
   `MigrationsTests.MigrateAsync_on_an_empty_database_applies_every_migration` — the test now compares
   applied vs. assembly migrations and passes), and `lockflight init` to re-pin the engine (doctor FAIL).
2. User: review sprint-1 content, flip `reviewed`; then `uk` pass for reviewed items.
3. Content: Angular (3) and AI engineering (5) domains.
4. 005 LLM grading (open + kata) — readiness widens automatically when open questions become schedulable.

**Gotchas learned in 004**
- Lockflight red classifier: only xUnit `Assert.*` without a custom message counts as RED_ASSERT; Shouldly
  failures in test code are RED_IN_TEST. In API tests, assert the status with `Assert.Equal` first.
- `phase check tasks` recognises test tasks under a `### Tests` heading or starting with "Tests"; stub tasks
  must contain the word "stub"; FR ids go right after the story label.
- Test projects lock after `redgreen red`; a test that must change (e.g. a hard-coded migration count)
  needs the user's `lockflight baseline tests` or `redgreen reset`.
