# Feature Specification: Knowledge atlas and concept notes

**Feature Branch**: `004-knowledge-atlas-notes`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User description: "004 Knowledge atlas & notes — the rail's "Atlas" section. Browse domain →
topic → concept in the Atlas design (ADR 0006), render the concept note (Markdown with code
highlighting), EN/UK via the existing runtime language switch with per-item English fallback (and the
"unreviewed" badge), show readiness per domain and per topic as % of concepts that are "ready"
(review_state ease >= 2.5 and interval >= 7 days across the concept's questions), and from any
domain/topic/concept start a review session scoped to it. Reuse the existing knowledge read endpoints
from 001 (tree + concept page) and extend them with lang and readiness."

## Clarifications

### Session 2026-10-15

- Q: When is a concept "ready"? → A: When **every** question of the concept has a review state with
  ease ≥ 2.5 and interval ≥ 7 days. A concept with at least one review state that is not ready is "in
  progress"; one with no review state at all is "not started".
- Q: Is a review session scoped to a single concept in scope? → A: Yes, in 004 — a new session scope
  "concept" next to all / domain / topic.
- Q: Which questions count for readiness, given that review sessions only schedule multiple-choice
  questions until feature 005? → A: Only the questions the review flow can schedule (today:
  non-deprecated multiple-choice questions). A concept with none of them counts as a concept without
  questions. When later features schedule more question types, readiness covers them automatically.
- Q: The scheduler never raises ease after a wrong answer, so "ease ≥ 2.5" would mark a question
  not ready forever after one miss. Which rule? → A: Interval only: a question is ready when its
  review state has an interval ≥ 7 days (amends the first answer above; ease is not used).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse the atlas down to a concept (Priority: P1)

The learner opens "Atlas" from the spine rail (today marked "soon") and sees every domain in manifest
order with its number and color, each with its topics; opening a topic lists its concepts with title,
level (M/S) and question count. Opening a concept leads to its note. Breadcrumbs (Atlas › domain ›
topic › concept) and the rail's domain tabs keep the learner oriented; every level has its own URL so
it can be bookmarked and reloaded.

**Why this priority**: The notes are the study material; without a way to reach them the content
pipeline only feeds quizzes.

**Independent Test**: With imported content and an empty progress table, open `/atlas`, drill into a
domain, a topic and a concept, reload on each level and use the browser back button.

**Acceptance Scenarios**:

1. **Given** imported content, **When** the learner opens the Atlas, **Then** domains and topics are
   listed in manifest order and deprecated ones are hidden.
2. **Given** a topic without any content file yet, **When** it is shown, **Then** it is visibly marked
   as "soon" and cannot be opened.
3. **Given** the learner is on a concept URL, **When** the page is reloaded, **Then** the same concept
   is shown with the same breadcrumbs.
4. **Given** an unknown or deprecated domain/topic/concept URL, **When** it is opened, **Then** a
   "not found" state with a link back to the Atlas is shown.

---

### User Story 2 - Read a concept note (Priority: P1)

The concept page renders the note as formatted text: headings, lists, emphasis, tables, inline code and
fenced code blocks with syntax highlighting for the languages used in the content (C#, TypeScript,
SQL, JSON, shell). Under the note the learner sees the concept's level, question/kata counts and the
neighbouring concepts (previous / next within the topic).

**Why this priority**: Reading the note is the core value of the feature.

**Independent Test**: Open a concept whose note contains a heading, a list and a C# code block; check
the rendering and highlighting in light and dark themes; follow "next" to the following concept.

**Acceptance Scenarios**:

1. **Given** a note with a fenced `csharp` block, **When** the concept page opens, **Then** the block
   is shown monospaced with syntax highlighting legible in both themes.
2. **Given** a note containing raw HTML or a script tag, **When** it is rendered, **Then** no script
   runs and no unsafe markup reaches the page.
3. **Given** the last concept of a topic, **When** its page is shown, **Then** "next" is absent and
   "previous" leads to the preceding concept.

---

### User Story 3 - Ukrainian, fallback and review status (Priority: P1)

With the language switched to УК, domain/topic titles, concept titles and notes appear in Ukrainian
where a translation exists; anything served in English instead carries the "EN" badge. Concepts that
are not human-reviewed carry the "unreviewed" badge in lists and on the concept page. Switching the
language on a concept page re-renders it in place without losing the position.

**Why this priority**: The UI is already bilingual (003); the Atlas must not regress it.

**Independent Test**: Switch to УК on a concept that has a `uk` note and on one that has not; check
the text and badges; switch back.

**Acceptance Scenarios**:

1. **Given** a concept with a Ukrainian note, **When** the language is УК, **Then** the Ukrainian
   title and note are shown without an "EN" badge.
2. **Given** a concept without a Ukrainian note, **When** the language is УК, **Then** the English
   note is shown with an "EN" badge.
3. **Given** an unreviewed concept, **When** it appears in a list or on its page, **Then** it carries
   the "unreviewed" badge in either language.

---

### User Story 4 - See readiness per domain, topic and concept (Priority: P2)

Each domain and topic shows a readiness percentage and bar: the share of its concepts that are
"ready". Each concept shows whether it is ready, in progress, or not started. Readiness reflects the
learner's review history and changes after review sessions.

**Why this priority**: It turns the atlas into a map of what is still weak, but browsing and reading
work without it.

**Independent Test**: Seed review states for one topic so that some concepts meet the readiness rule
and others do not; check the percentages on the domain and topic and the per-concept states.

**Acceptance Scenarios**:

1. **Given** a topic with 4 concepts that have questions, 1 of them ready, **When** the topic is
   shown, **Then** its readiness is 25%.
2. **Given** a concept with questions but no review history, **When** it is shown, **Then** it is
   "not started" and counts as not ready.
3. **Given** a concept without questions, **When** readiness is computed, **Then** it is excluded
   from both the numerator and the denominator.
4. **Given** a domain with no concept that has questions, **When** it is shown, **Then** no
   percentage is displayed (not "0%").

---

### User Story 5 - Start a review from where I am (Priority: P2)

From a domain, a topic or a concept the learner can start a review session limited to it. The action
leads to the existing review flow with that scope preselected, so the usual start/limits/resume rules
apply.

**Why this priority**: Closes the loop read → practise; it reuses the existing session machinery.

**Independent Test**: From a topic page, start a review; check that the session only contains that
topic's questions; repeat from a domain and from a concept.

**Acceptance Scenarios**:

1. **Given** a topic page, **When** the learner starts a review, **Then** the review flow opens with
   that topic preselected.
2. **Given** a concept page, **When** the learner starts a review, **Then** the session contains only
   that concept's questions.
3. **Given** an open session already exists, **When** a scoped review is started, **Then** the
   existing resume behavior of the review flow applies (no second open session).

---

### Edge Cases

- A concept note is empty or missing in English: the page shows a "no note yet" state (should not
  happen after schema validation, but the UI must not break).
- Very long notes and wide code blocks: code scrolls horizontally inside its block; the page itself
  never scrolls horizontally at phone width.
- Content re-imported while the page is open: the next navigation shows the new content; no stale
  cache survives a reload.
- A concept becomes deprecated: it disappears from lists; its URL shows "not found" (or the
  deprecated view only when explicitly requested).
- Language switch while the note is loading: the page ends in the latest selected language.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Atlas MUST list all non-deprecated domains and topics in manifest order with their
  number, color, title and concept count, and mark topics without content as "soon".
- **FR-002**: The Atlas MUST provide one addressable view per level (atlas, domain, topic, concept)
  that survives reload and supports back/forward navigation.
- **FR-003**: The concept view MUST render the note's Markdown (headings, lists, emphasis, tables,
  links, inline code, fenced code) with syntax highlighting for C#, TypeScript, JavaScript, SQL, JSON,
  HTML and shell.
- **FR-004**: Note rendering MUST neutralise raw HTML and scripts in the content.
- **FR-005**: The concept view MUST show level, question count, kata count, and links to the previous
  and next concept within the same topic.
- **FR-006**: All titles and notes MUST follow the selected UI language with per-item English fallback;
  items served in English while Ukrainian is selected MUST carry the "EN" badge.
- **FR-007**: Unreviewed concepts MUST carry the "unreviewed" badge wherever they are listed or shown.
- **FR-008**: The system MUST compute per concept a readiness state — not started / in progress /
  ready — where "ready" means every schedulable question of the concept (today: every non-deprecated
  multiple-choice question) has a review state with an interval ≥ 7 days, "in progress" means at least
  one review state exists but the concept is not ready, and "not started" means no review state exists;
  concepts without schedulable questions have no state.
- **FR-009**: The system MUST show readiness per topic and per domain as the percentage of their
  concepts with schedulable questions that are ready (rounded down, so 100% only when all are
  ready), and no percentage when no such concept exists.
- **FR-010**: Readiness MUST be computed from the single local learner's review states only and MUST
  NOT depend on the selected language.
- **FR-011**: Users MUST be able to start a review session scoped to a domain or a topic from its view,
  landing in the existing review flow with the scope preselected.
- **FR-012**: Users MUST be able to start a review session scoped to a single concept from its view,
  using a session scope "concept" that contains only that concept's non-deprecated questions.
- **FR-013**: The rail's "Atlas" item MUST become active (no longer "soon") and open the Atlas.
- **FR-014**: All new UI copy MUST exist in both EN and UK dictionaries and follow the Atlas tokens in
  light and dark themes (ADR 0006).
- **FR-015**: Unknown or deprecated paths MUST show a "not found" state linking back to the Atlas.

### Key Entities *(include if feature involves data)*

- **Domain / Topic / Concept**: existing content graph (manifest order, titles, optional Ukrainian
  titles, deprecation); unchanged by this feature.
- **Concept readiness**: derived, not stored — the state of a concept for the local learner computed
  from the review states of the concept's questions.
- **Readiness summary**: derived per topic and per domain — ready count, counted concepts, percentage.
- **Review scope**: existing session scope (all / domain / topic), extended with concept.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From the Atlas the learner reaches any concept note in at most 3 selections.
- **SC-002**: A concept page with a 900-word note and code blocks is readable within 1 second on the
  local stack after navigation.
- **SC-003**: Readiness percentages shown for a seeded fixture match a hand calculation for every
  domain and topic.
- **SC-004**: Switching EN ↔ УК on any Atlas view re-renders it in place, with no reload and no lost
  position.
- **SC-005**: A review started from a topic and from a concept contains only questions
  of that scope in 100% of checked sessions.
- **SC-006**: All Atlas views pass a keyboard-only walkthrough and have no horizontal page scroll at
  375 px width.

## Assumptions

- Content changes, imports and the review session flow from 001–003 are reused as they are; this
  feature adds reading and readiness, not new content fields.
- Ukrainian domain/topic titles come from the manifest work done alongside this feature (deferred item
  from 003); until a title has a Ukrainian value it falls back to English.
- Kata pages, LLM grading and weak-spot reviews are out of scope (features 005 and 006); kata counts
  are shown only as numbers.
- No search, bookmarks or "mark as read" in this feature.
- Readiness uses the fixed threshold interval ≥ 7 days (three correct answers in a row since the last
  miss with the current scheduler); it is not user-configurable.
- Single local learner; no authentication.
