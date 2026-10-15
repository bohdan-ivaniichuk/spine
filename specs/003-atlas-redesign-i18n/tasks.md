# Tasks: Atlas redesign, Ukrainian UI and the Spine name

**Input**: `specs/003-atlas-redesign-i18n/` — spec.md, contracts/review-api-i18n.md; ADR 0006.

**Organization**: Backend (`src/**`, `tests/**`) and Frontend (`web/**`) in parallel, then the rename,
then integration.

## Backend

- [X] T001 Move `LanguageQuery` to `src/Api/Endpoints/LanguageQuery.cs` with `LanguageQueryValidator<T>`; knowledge endpoints use `KnowledgeQuery`
- [X] T002 `lang` on all review endpoints; `TranslationPicker` (Application) serves a question in uk only when uk options align with en; concept titles fall back individually
- [X] T003 `language` / `fallback` on `SessionQuestionDto` and `AnswerFeedbackDto`
- [X] T004 `GET /api/review/coverage` (two no-tracking queries, manifest order)
- [X] T005 Tests: `TranslationPickerTests`, `ReviewSessionsLanguageTests`, `ReviewI18nEndpointTests`, `CoverageEndpointTests` (210 → 237)
- [X] T006 Contract `contracts/review-api-i18n.md`

## Frontend

- [X] T010 `shared/i18n`: `I18n` service (signal, persistence, `<html lang>`, Intl formatting), `plural`, `withLang`, typed `messages.en.ts` / `messages.uk.ts`, `I18nTitleStrategy`, `LanguageToggle`
- [X] T011 Atlas tokens, dark Atlas, fonts with Cyrillic, `domain-1…5` utilities in `styles.scss` (contrast measured, ADR 0006)
- [X] T012 Shell: spine rail in `app.html` (sections, domain tabs, language, theme, API status); `KnowledgeTreeSource` shared root resource
- [X] T013 Overview: hero, ink Today card, atlas tiles with coverage, `?scope=&domain=&topic=` presets, atlas click selects scope
- [X] T014 Session: session list, Atlas question card (domain chip, EN fallback badge), option rows, ink feedback; store re-reads session/summary on language change
- [X] T015 Summary and concept peek in i18n and Atlas style
- [X] T016 Tests: i18n spec, rail + Ukrainian shell, atlas/presets/Ukrainian overview; existing 002 specs adapted (68 → 78)

## Rename

- [X] T020 `InterviewPrep.*` → `Spine.*`: solution, projects, namespaces, Dockerfiles, compose project (data volume kept), docs, memory-bank; storage keys `spine.*`
- [X] T021 User: protected files (`Directory.Build.props` RootNamespace, `.claude/**` texts)

## Integration

- [X] T030 Rebuild the stack, walk Review → session → summary in Chrome in EN/UK and light/dark
- [X] T031 `code-reviewer` (done: clean) and `frontend-reviewer`; fix must-fix
- [X] T032 memory-bank, CLAUDE.md; commit on `dev`, merge to `main`
