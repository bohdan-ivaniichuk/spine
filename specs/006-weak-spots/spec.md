# Feature Specification: Weak spots

**Feature Branch**: `006-weak-spots`

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "006 Weak spots — help the learner find and drill what they get wrong. (1) A
"Weak spots" view that ranks the learner's weakest concepts and topics from their answer history:
accuracy, wrong answers, last seen, readiness state, with links to the concept note in the Atlas and a
one-click drill. (2) A weak-spots review session: a session mode that picks questions the learner
recently answered wrong, optionally limited to a domain/topic/concept scope, using the existing session
flow, limits, resume and scheduling. (3) Session summary weak concepts link to their concept pages. No
LLM."

## Clarifications

### Session 2026-10-27

- Q: Which questions go into a weak-spots session? → A: Questions whose **most recent answer was wrong**,
  whether or not they are due. A question leaves the weak set as soon as it is answered correctly.
- Q: How are the weakest concepts ranked? → A: By accuracy over the **last 30 days**, counting only
  concepts with at least 3 answers in that window; lower accuracy first, ties broken by more wrong answers.
- Q: When does a topic qualify for the topic ranking? → A: When its concepts together have at least 3
  answers in the window (a topic does not need a qualifying concept of its own).
- Q: Is the weak mode visible during the session? → A: Yes, the session page marks a weak session.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Drill what I last got wrong (Priority: P1)

The learner starts a "weak spots" review: a session made only of questions whose most recent answer was
wrong, regardless of their due dates. It runs through the usual review flow — one question at a time,
feedback, resume, summary — and every answer updates the schedule as usual. The session can be limited
to a domain, a topic or a concept, like any other review.

**Why this priority**: Re-answering exactly what was missed is the fastest way to close gaps before an
interview; it needs nothing but the answer history the app already keeps.

**Independent Test**: Answer some questions wrong and some right in a normal session, then start a weak
session: it contains exactly the wrongly answered questions; answer one correctly and start again — it is
gone.

**Acceptance Scenarios**:

1. **Given** questions whose last answer was wrong, **When** the learner starts a weak session, **Then**
   the session contains only those questions, most recently missed first, up to the session limit.
2. **Given** a question answered wrong and later answered right, **When** a weak session is started,
   **Then** that question is not included.
3. **Given** a weak session limited to a topic, **When** it starts, **Then** it contains only that
   topic's wrongly answered questions.
4. **Given** no question in scope has a wrong last answer, **When** the learner tries to start a weak
   session, **Then** they are told there is nothing to drill and no session is created.
5. **Given** an open session of any kind, **When** a weak session is started, **Then** the existing
   "one open session" behaviour applies.

---

### User Story 2 - See where I am weak (Priority: P1)

A "Weak spots" section in the rail shows the learner's weakest concepts, ranked by accuracy over the last
30 days, each with its domain/topic, accuracy, number of wrong answers, when it was last answered, its
readiness state and how many of its questions currently have a wrong last answer. Each entry links to the
concept note and offers a one-click drill of that concept. Weakest topics are shown the same way. A
header offers a weak session across everything.

**Why this priority**: The learner needs to know where to look before deciding what to drill or reread.

**Independent Test**: Seed an answer history with known accuracies; open the view and compare the order
and figures with a hand calculation.

**Acceptance Scenarios**:

1. **Given** concepts with at least 3 answers in the last 30 days, **When** the view opens, **Then** they
   are listed by accuracy ascending, ties by more wrong answers first.
2. **Given** a concept with only 2 answers in the window, **When** the view opens, **Then** it is not
   ranked.
3. **Given** answers older than 30 days, **When** accuracy is computed, **Then** they are ignored.
4. **Given** an entry, **When** the learner follows its note link or drill action, **Then** they land on
   the concept page or on review with a weak session scoped to that concept preselected.
5. **Given** no answer history yet, **When** the view opens, **Then** an empty state explains that weak
   spots appear after reviewing and links to Review.

---

### User Story 3 - From the summary to the note (Priority: P2)

On the session summary, each weak concept links to its concept page in the Atlas, and a "drill these"
action starts a weak session over the questions just missed.

**Why this priority**: Closes the loop review → read → practise right where the learner sees the misses.

**Independent Test**: Finish a session with misses; follow a weak concept link and the drill action.

**Acceptance Scenarios**:

1. **Given** a summary with weak concepts, **When** the learner selects one, **Then** its concept page
   opens.
2. **Given** a summary with at least one wrong answer, **When** the learner chooses to drill, **Then** a
   weak session over the session's scope starts.

---

### Edge Cases

- A wrongly answered question is later deprecated: it is excluded from weak sessions and statistics.
- The same question answered wrong in several sessions counts once in "currently wrong" but every answer
  counts in accuracy.
- A weak session is resumed after a question in it was answered correctly elsewhere: the frozen session
  membership stays as it was (existing rule).
- Language switch: figures and ranking never change; titles follow the selected language with the
  usual English fallback.
- Very long history: the view stays responsive and shows at most the top 20 concepts and top 10 topics.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST offer a weak review mode whose sessions contain only non-deprecated,
  schedulable questions in scope whose most recent answer was wrong, regardless of due date.
- **FR-002**: Weak sessions MUST order questions by the time of the wrong answer, most recent first, and
  respect the usual total limit.
- **FR-003**: Weak sessions MUST be combinable with every existing scope (all, domain, topic, concept).
- **FR-004**: Answers in weak sessions MUST update scheduling and history exactly like any other session, and the session MUST show that it is a weak session.
- **FR-005**: Starting a weak session with nothing to drill MUST be refused with a clear message and no
  session created.
- **FR-006**: The overview for a scope MUST show how many questions are currently weak, so the learner
  sees whether a weak session is possible.
- **FR-007**: The system MUST compute per concept, over the learner's answers in the last 30 days:
  answers, wrong answers, accuracy, and last answered time; plus how many of its questions currently have
  a wrong last answer.
- **FR-008**: The weak-spots view MUST rank concepts with at least 3 answers in the window by accuracy
  ascending, ties by wrong answers descending then by most recent answer, showing at most 20.
- **FR-009**: The view MUST rank topics the same way over their concepts' answers combined, qualifying a
  topic when it has at least 3 answers in the window, showing at most 10.
- **FR-010**: Each ranked concept MUST link to its concept page and offer a drill that opens review with
  a weak session scoped to that concept preselected; each topic offers the same for the topic.
- **FR-011**: Each ranked concept MUST show its readiness state as defined in feature 004.
- **FR-012**: Statistics MUST use only the single local learner's answers and MUST NOT depend on the
  selected language.
- **FR-013**: The session summary MUST link each weak concept to its concept page and offer a drill of
  the session's scope in weak mode when the session had wrong answers.
- **FR-014**: The rail MUST show an active "Weak spots" section; all new copy MUST exist in EN and UK and
  follow the Atlas tokens in both themes.
- **FR-015**: The weak-spots view MUST show an empty state with a link to Review when there is no ranked
  concept.

### Key Entities

- **Answer (attempt)**: existing record of each answer with time and correctness; source of all
  statistics.
- **Weak question**: derived — a schedulable question whose most recent answer was wrong.
- **Concept / topic weakness**: derived — answers, wrong answers, accuracy and last answered time over the
  last 30 days, plus the count of weak questions.
- **Session mode**: new attribute of a review session — regular or weak — next to its scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A weak session started after a review with misses contains exactly the missed questions in
  100% of checked cases.
- **SC-002**: The ranking shown for a seeded history matches a hand calculation for every listed concept
  and topic.
- **SC-003**: From the weak-spots view the learner reaches a concept note or starts a drill in one
  selection.
- **SC-004**: The weak-spots view is usable within 1 second on the local stack with a year of daily
  reviews.
- **SC-005**: All new views pass a keyboard-only walkthrough and have no horizontal page scroll at 375 px
  in both languages and themes.

## Assumptions

- Only multiple-choice answers exist until LLM grading (005); open questions and katas enter weak sessions
  and statistics when they become reviewable.
- The 30-day window and the 3-answer minimum are fixed, not user-configurable.
- Weak sessions reuse the existing limits; the "new questions" limit does not apply (weak questions are
  never new).
- No new content fields; no notifications or reminders.
- Single local learner; no authentication.
