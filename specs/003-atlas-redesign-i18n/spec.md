# Feature Specification: Atlas redesign, Ukrainian UI and the Spine name

**Feature Branch**: `003-atlas-redesign-i18n`

**Created**: 2026-10-15

**Status**: Implemented

**Input**: User request (2026-10-14/15): the 002 look is dated; redesign it so the resource feels
attractive, add Ukrainian next to English, rename the product. Directions were explored on the design
canvas "InterviewPrep Redesign"; the user chose **B · Atlas** with the **B1 spine rail** navigation and
the name **Spine**, and asked for the project, solution and namespaces to be renamed as well.

## Clarifications

### Session 2026-10-15

- Q: Which visual direction? → A: B · Atlas (light ground, ink panels, five domain colors, Unbounded
  display type) with the B1 "spine" rail instead of a top header. Dark theme is an Atlas variant.
- Q: How does the language switch work? → A: At runtime, no reload: typed EN/UK dictionaries behind
  a signal; the same language is sent to the API (`?lang=uk`) so content follows with per-item
  English fallback. Choice persists per browser.
- Q: What is translated now? → A: All UI copy. Content (prompts, options, explanations, concept
  titles and notes) is served in Ukrainian where a `uk` translation exists; none exists yet (the `uk`
  pass runs after human review), so content shows in English with an "EN" badge. Domain and topic
  titles stay English until the manifest carries `uk` titles (deferred).
- Q: Name? → A: "Spine — the senior engineer's interview atlas". Solution, projects and namespaces
  become `Spine.*`. The PostgreSQL database and role keep the name `interviewprep` (they live in the
  user's `.env` and data volume).

## User Scenarios & Testing

### User Story 1 - A home that feels like an atlas (P1)

The learner lands on Review and sees a hero, a dark "Today" card with new/due counts, scope and
limits and a Start button, and below it the atlas: one tile per domain (number 01–05, its color),
each listing its topics with seen/eligible progress and due badges; topics without questions say
"soon". Clicking a domain or topic tile selects that scope in the Today card.

**Acceptance**: tiles follow manifest order; progress comes from `GET /api/review/coverage`;
selecting a topic requests that topic's counts; deprecated topics never show.

### User Story 2 - Navigate by the spine (P1)

A rail on the left (a bar on top on small screens) holds the Spine mark, sections (Review; Atlas
and Katas marked "soon"), the five domain tabs 01–05 in their colors, the language switch, the theme
switch and the API status. A domain tab opens Review with that domain preselected
(`/review?scope=domain&domain=…`).

### User Story 3 - Use the app in Ukrainian (P1)

The learner switches EN → УК anywhere; every label, plural, date and page title changes at once,
an open session keeps its position and answers and re-reads its text in Ukrainian, and requests
carry `lang=uk`. Questions without a Ukrainian translation show an "EN" badge.

**Acceptance**: Ukrainian plural rules (1 картка / 3 картки / 5 карток / 11 карток); `<html lang>`
follows; the choice survives a reload; a UK dictionary key exists for every EN key.

### User Story 4 - Session and summary in the new look (P2)

The session shows the session list (✓ / ✗ / position per concept) beside the card, options as
rounded rows with A–D circles (blue = right, orange = wrong, plus ✓/✗ marks), and the feedback in an
ink panel. The summary uses the same type and colors. Keyboard behavior from 002 is unchanged.

## Requirements

- **FR-001** Every review endpoint accepts `lang=en|uk` (default `en`, anything else → 400) and
  returns text in that language with per-item English fallback; a question is served in Ukrainian
  only when its Ukrainian options align with the English ones (same count, same correct index).
- **FR-002** Session questions and answer feedback carry `language` and `fallback`.
- **FR-003** `GET /api/review/coverage` returns eligible/seen/due counts per non-deprecated topic in
  manifest order (contract: `contracts/review-api-i18n.md`).
- **FR-004** Grading, scheduling and session membership do not depend on language.
- **FR-005** UI copy lives only in `web/src/app/shared/i18n/messages.{en,uk}.ts`; components never
  hard-code user-visible English.
- **FR-006** Colors come from tokens (ADR 0006); answer states differ in lightness, not hue alone,
  and every text/background pair meets WCAG AA in both themes.
- **FR-007** Product name "Spine" in UI, titles and docs; code namespaces `Spine.*`.

## Success Criteria

- **SC-001** Switching language re-renders the visible screen without a reload and without losing
  an open session's progress.
- **SC-002** All token text pairs ≥ 4.5:1 in light and dark (measured, ADR 0006).
- **SC-003** All existing 002 behavior tests still pass; new tests cover fallback, coverage,
  plurals, the rail and the Ukrainian overview.

## Out of scope

Ukrainian content itself (content workflow), Ukrainian domain/topic titles (manifest + importer +
migration), the Atlas and Katas pages (features 004 and 005).
