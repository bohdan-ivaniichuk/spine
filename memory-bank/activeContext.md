# Active Context — InterviewPrep

## Where things stand (2026-10-14, branch `dev`)

Features **001 Foundation** and **002 Review session** are complete. 002 ships the daily MCQ review
loop: overview with due/new counts per scope, server-built session with membership frozen on the
session row (ADR 0005), SM-2 behind `ISchedulingAlgorithm`, instant feedback, resume after reload,
summary with weak concepts (expandable in place until 003) and "due tomorrow".
Tests: 210 .NET (20 domain, 70 application, 62 API, 58 infrastructure) + 68 web, all green; both
reviewers clean; walked end-to-end in Chrome in both themes. Lockflight (lite) adopted 2026-10-14.

**Done in the 002 close-out (2026-10-14)**
- Fixed nginx `/api/` proxy: with a variable in `proxy_pass`, a URI part replaced the whole request
  URI, so every API call except `/api/health` returned 404 behind nginx (latent since 001).
- Backend: session row locks (`FOR UPDATE`) in Answer/Finish/Start (Start locks before planning);
  review_states unique violation → `DuplicateAnswer`; 422 "Question deprecated"; empty-plan Start
  keeps the open session; SM-2 interval capped at 36500 days.
- Web: radio-group keyboard pattern (one Tab stop, arrows), ✓/✗ marks (not color-only), held-Enter
  guard, focus stays on the error after a failed finish, summary no longer blanks on "Review again",
  weak concepts expand in place via the concept read endpoint.
- Light-theme `ok`/`bad` darkened for AA (ADR 0004 amendment).

**Next**
1. Human review of `working/content-review-digest-clr-memory.md` and `…-async.md` → flip
   `reviewed: true` → re-import.
2. Content sprint (see progress.md), then 003 Knowledge tree & notes (replaces the summary's
   in-place concept peek with the concept route; adds the Playwright smoke deferred from 002).

**Waiting on the user**
- Content review of `clr-memory` and `async`.
