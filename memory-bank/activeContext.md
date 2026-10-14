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
- 004 spec written and clarified (`specs/004-knowledge-atlas-notes`): readiness = every question of the
  concept has ease ≥ 2.5 and interval ≥ 7 days; new session scope `concept`.

**Next**
1. User: `~/.lockflight/bin/lockflight approve spec` for 004 (`dotnet lockflight` fails — the pinned
   `lockflight.cli` dev version is not on NuGet). Then plan → tasks → approve tasks → red/green.
2. User: review sprint-1 content, flip `reviewed`; then `uk` pass for reviewed items.
3. User: protected-file edits for the rename (003 T021), including the `RootNamespace` patch in the
   scratchpad — until then fix the namespace of every new EF migration by hand.
4. Content: Angular (3) and AI engineering (5) domains.

**Waiting on the user**
- 004 spec approval; sprint-1 content review; protected-file edits.
