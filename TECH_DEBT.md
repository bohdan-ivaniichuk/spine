# InterviewPrep — Tech Debt

**How to read:** grouped by area, sorted by severity within each area. Each item lists the file(s), the
problem, why it matters, and a rough effort (`XS` < 1h, `S` 1–4h, `M` 1–2d, `L` 3–5d).
Add items as they are discovered; remove them when fixed (with the commit).

## Severity quick index

| Sev | Count | Top items |
|---|---|---|
| Critical | 0 | — |
| High | 0 | — |
| Medium | 3 | Light-theme contrast below AA; Start/Answer race; retried commit can 409 |
| Low | 10 | Radio-group keyboard pattern; limits hard-coded in web; grading rules in Infrastructure |

## 1. Content pipeline

_1.1 (schema validation runner) closed by feature 001: `scripts/validate-content.sh` runs the same
validator as the importer (`import-content --validate-only`)._

## 2. Review session (feature 002, from pre-commit reviews)

### 2.1 [MEDIUM] Light-theme colour pairs below WCAG AA for small text
- **Files:** `web/src/styles.scss` (light palette)
- **Problem:** `text-bad` on `bg-bad-soft`/`bg-surface-2`/`bg-surface` (~3.8–4.4:1), `text-muted` on
  `bg-surface-2`/`bg-accent-soft` (~4.2–4.3:1), `text-ok` hint (~3.5:1). Dark theme passes.
- **Fix:** darken `--color-bad`, `--color-muted`, `--color-ok` in the light theme; no component change.
- **Effort:** XS

### 2.2 [MEDIUM] Start and Answer are not serialised
- **Files:** `src/Infrastructure/Review/ReviewSessions.cs` (Start ~80, Answer ~150)
- **Problem:** Answer reads `session.FinishedAt` without a lock; a concurrent Start can close the session
  and freeze its counts while the answer still lands, leaving `answered_count`/`correct_count` stale.
- **Fix:** take the same advisory lock in Answer, or read the session `FOR UPDATE`.
- **Effort:** S

### 2.3 [MEDIUM] Retried commit can turn a stored answer into 409
- **Files:** `src/Infrastructure/Review/ReviewSessions.cs` (~142–230, Start ~70–115)
- **Problem:** with `EnableRetryOnFailure`, a transient error after Postgres commits re-runs the delegate;
  the duplicate check then answers 409 (Start could close the session it just created).
- **Fix:** `ExecuteInTransactionAsync` with a verify-succeeded callback, or accept for single-user.
- **Effort:** S

### 2.4 [LOW] Duplicate-answer catch matches only one constraint
- **Files:** `src/Infrastructure/Review/ReviewSessions.cs:222`
- **Problem:** only `ix_attempts_session_id_item_id` maps to 409; if EF ordered the `review_states` insert
  first, `ix_review_states_user_id_item_id` would surface as 500. Catch both.
- **Effort:** XS

### 2.5 [LOW] Answers to skipped (deprecated) questions are accepted
- **Files:** `src/Infrastructure/Review/ReviewSessions.cs:157`
- **Problem:** only `QuestionIds` membership is checked; `SkippedQuestionIds` are still recorded and
  scheduled, inconsistent with FR-011. Reject with `QuestionNotInSession` or document.
- **Effort:** XS

### 2.6 [LOW] Grading/eligibility rules live in Infrastructure
- **Files:** `src/Infrastructure/Review/ReviewSessions.cs` (~178, ~283–336, ~487–498)
- **Problem:** correct-option lookup, correct → Good/Again mapping and new/eligible rules sit in the EF
  class; move pure parts to Domain/Application for Docker-free unit tests.
- **Effort:** S

### 2.7 [LOW] Small backend clean-ups
- `HadState` uses strict `<` on `CreatedAt` (fragile with `FakeTimeProvider` without `Advance`).
- `tests/Api.Tests` references `Microsoft.Extensions.TimeProvider.Testing` without using it.
- DB defaults 20/10 in `ProgressConfigurations.cs` duplicate `ReviewOptions` defaults.
- Explicit `Microsoft.EntityFrameworkCore.Relational` reference is redundant unless the pin is intended.
- Non-numeric `REVIEW_DEFAULT_*` fails at startup without naming the setting.
- `Session.QuestionIds` is a settable array despite "never changes".
- **Effort:** XS each

### 2.8 [LOW] Option list is not an ARIA radiogroup pattern; focus lost while checking
- **Files:** `web/src/app/review/option-list/option-list.html`, `review-session/review-session.html`
- **Problem:** each `role=radio` is a Tab stop (no roving tabindex/arrow keys); during `checking` the
  buttons disable and focus drops to `<body>`, not restored to Check after a failure (FR-015 retry).
- **Effort:** S

### 2.9 [LOW] Overview limits and errors
- **Files:** `web/src/app/review/review-overview/*`
- **Problem:** 1..100 hard-coded although the API returns `limits`; no `newLimit <= totalLimit` check;
  a failed resume probe shows a second error that Retry does not clear; Start silently ends an
  unfinished session while the Resume banner is visible.
- **Effort:** S

### 2.10 [LOW] Unrouted links fall back to /review silently
- **Files:** `web/src/app/app.routes.ts:9`, `session-summary.html:45`
- **Problem:** Knowledge/Katas and weak-concept links hit the `**` redirect. Add placeholder routes
  ("coming in 003") or disable the links.
- **Effort:** XS

### 2.11 [LOW] Web nits
- Review pages imported eagerly in `review.routes.ts` (use `loadComponent`).
- Feedback panel is both `aria-live` and focused (possible double announcement).
- `hover:bg-surface-2` still applies to disabled option rows.
- Shortcut hint omits A–D and is `aria-hidden`.
- ESLint is not configured (`ng lint` prompts to add angular-eslint), so `web-format.sh` lint is a no-op.
- **Effort:** XS each

## 3. Tooling

### 3.1 [LOW] Monaco wrapper choice unverified
- **Files:** `memory-bank/techContext.md`
- **Problem:** `ngx-monaco-editor-v2` version came from search snippets (npm returned 403 to the
  researcher). Decide between the wrapper and loading `monaco-editor` directly in a `@defer` block
  during feature 004.
- **Effort:** XS (decide at 004 planning)
