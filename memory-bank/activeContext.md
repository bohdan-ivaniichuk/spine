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

- **006 Weak spots done** (ADR 0008, 2026-10-28): `sessions.mode` (`regular`/`weak`, migration `AddSessionMode`);
  weak = latest attempt `is_correct = false` (from attempts, not SM-2), `ReviewQueryables.WeakQuestions` anti-join on
  `item_id`; `StartWeakAsync` shares `StartCoreAsync` (same locks), 409 "Nothing to drill"; overview `weakCount`;
  `GET /api/review/weak-spots` (30-day window rounded up to the minute, ≥ 3 answers, top 20 concepts / 10 topics,
  pure `WeaknessRanker`, readiness reused). Web: `/weak`, rail item, overview mode toggle (`?mode=weak`), weak chip,
  summary note links + "Drill these". 356 .NET + 188 web tests; trace 11/15 (web-only FRs untraced); Lockflight
  review of record: approve; walked in Chrome (drill → answers → summary → drill again, EN/UK, dark, 375 px).

- **Public release (2026-11-03)**: github.com/bohdan-ivaniichuk/spine, MIT, `main` protected (PR + required `gates`
  check, merge commits). First CI run caught a Linux-only clock-precision bug (fixed with `PostgresTimeProvider`).
- **007 Activity and streak** (2026-11-03, first feature fully under Lockflight 0.3.0 + Spec Kit): `GET
  /api/review/activity?tz=` (UTC default, IANA-only, 400 otherwise), pure `ActivityCalculator`, overview streak card
  with 14-day chart and sr-only text alternative; Alpine API image gains `tzdata`. Trace 5/9 (web FRs untraced);
  review of record approve; 381 .NET + 202 web tests.

**Next**
1. Public release (2026-11-03): repo moved to `~/GitHub/public/spine`; MIT license, README with screenshots
   (`docs/images/`), CODEOWNERS, noreply commit email. Pending user steps: apply the CI trigger patch
   (PRs + push to main), run the email history rewrite, create `bohdan-ivaniichuk/spine` (public), push.
   Then dogfood Lockflight on real PRs. Tasks approvals for 004/006 are stale (checkboxes ticked after
   sign-off): the user re-approves them or leaves them as history.
2. User: review sprint-1 content, flip `reviewed`; then `uk` pass for reviewed items.
3. Content: Angular (3) and AI engineering (5) domains.
4. 005 LLM grading (open + kata) — readiness widens automatically when open questions become schedulable.

**Gotchas learned in 007 (Lockflight on a real PR)**
- Keep each story's tests in their own file from the start: the CI judge rebuilds a cycle's tests on its red commit,
  and later-story tests in the same file (calling members that did not exist yet) make that build compile-red;
  moving them afterwards renames them and loses their red.
- A test that only fails with ICU (Windows zone ids) is GREEN_BEFORE under `redgreen red` but red under plain
  `dotnet test`: environment-dependent behaviour cannot be proven red; fix it as hardening.
- Angular strips whitespace between elements: use `&ngsp;` where a space must be read (e.g. "4 days in a row").
- `<base href="/">` turns `href="#id"` into a full reload of `/#id`; focus targets in code instead.

**Gotchas learned in 004/006**
- (2026-11-03) Linux clocks tick at 100 ns, PostgreSQL keeps 1 µs: a time returned before SaveChanges differed from the
  re-read value (CI-only failure, macOS clocks are µs). The app clock is `PostgresTimeProvider.SystemTruncated`;
  integration tests that need this must wrap their FakeTimeProvider in it.
- Lockflight red classifier (alpha.6, fixed build): xUnit `Assert.*` without a custom message is RED_ASSERT; Shouldly
  failures in test code are RED_IN_TEST; a test must call production code (a production member, not just a type as a
  generic argument) or it is RED_NO_PRODUCTION — build API request bodies from the real request type.
- Cycle tests must pass for green; `baseline tests` does not excuse them. Keep time-dependent fields out of equality.
- `phase check tasks` recognises test tasks under a `### Tests` heading or starting with "Tests"; stub tasks
  must contain the word "stub"; FR ids go right after the story label.
- Test projects lock after `redgreen red`; a test that must change (e.g. a hard-coded migration count)
  needs the user's `lockflight baseline tests` or `redgreen reset`.
