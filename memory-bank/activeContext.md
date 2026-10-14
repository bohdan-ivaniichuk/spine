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

**Next**
1. User: update protected files after the rename (see 003 tasks T021).
2. Human review of the `clr-memory` and `async` digests → `reviewed: true` → re-import; then the
   `uk` translation pass for reviewed items (content-author) so Ukrainian content appears.
3. Ukrainian domain/topic titles: manifest `titles.uk` + importer + migration (deferred from 003).
4. Content sprint (domains 1, 2, 4), then 004 Knowledge atlas & notes (the rail's "Atlas" item).

**Waiting on the user**
- Protected-file edits for the rename; content review of `clr-memory` and `async`.
