# Feature Specification: Daily review session (MCQ + SM-2)

**Feature Branch**: `002-review-session`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "Daily review session (MCQ + SM-2): pick due questions (review_states.due_at <= now) plus new ones up to a limit, per domain/topic or all; MCQ UI with instant feedback and explanation; SM-2 update behind ISchedulingAlgorithm; session summary with weak concepts; Angular signals only. Only MCQ questions are scheduled in this feature (open questions and katas wait for 004). Include the review UI screens per the chosen design direction (ADR 0004)."

## Clarifications

### Session 2026-09-06

- Q: Which items are scheduled in this feature? → A: Multiple-choice questions only; open questions and katas are graded in feature 004 and become schedulable there (the review-state table already supports both).
- Q: How is an answer graded for spaced repetition? → A: Two outcomes. Correct = "good" (quality 4 in SM-2 terms); wrong = "again" (quality 1): repetitions reset, the question comes back tomorrow. No self-rated "easy/hard" buttons in this feature.
- Q: What happens to an unfinished session when a new one starts? → A: The open session is finished automatically (its summary is computed from the answers it has) and the new one starts; a learner never has two open sessions.
- Q: Where is session membership stored? → A: On the session row as an ordered list of question ids plus the two limits (additive columns); one answer per question per session is enforced by a database unique index.
- Q: Do unreviewed and deprecated questions appear in sessions? → A: Unreviewed questions do appear, with a visible "unreviewed" badge; deprecated questions never appear, and their existing review states are simply left alone.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Answer today's due questions with instant feedback (Priority: P1)

The learner opens the Review screen, sees how many questions are due today and how many new ones
are available, and starts a session. One question is shown at a time as a card: the prompt (with its
code snippet when there is one), four options, a Check action. After checking, the card immediately
shows whether the answer was right, highlights the correct option, and shows the explanation of why
each wrong option is wrong. Next moves to the following question. A progress indicator shows
position in the session (e.g. 7 / 20) and the session scope.

**Why this priority**: This is the core loop of the product — retrieval practice with immediate
feedback. Everything else in 002 supports it.

**Independent Test**: Import content, start a session, answer a few questions right and wrong,
confirm feedback and explanations appear, confirm progress advances, confirm answers are recorded.

**Acceptance Scenarios**:

1. **Given** imported content and no history, **When** the learner opens Review, **Then** they see
   "0 due · N new" for the chosen scope and can start a session.
2. **Given** a session in progress, **When** the learner selects an option and checks, **Then** the
   card shows correct/incorrect, marks the correct option, shows the explanation, and the Check
   action is replaced by Next; the answer cannot be changed afterwards.
3. **Given** a checked answer, **When** the learner presses Next (or Enter), **Then** the next
   question appears and the progress indicator advances.
4. **Given** a question has a code snippet in its prompt, **When** it is shown, **Then** the snippet
   is rendered as a code block with monospace text; options containing inline code show it as code.
5. **Given** the learner is mid-session and reloads the page, **When** the Review screen opens,
   **Then** the open session resumes at the first unanswered question (answers already given are kept).
6. **Given** an unreviewed question, **When** it is shown, **Then** a small "unreviewed" badge is
   visible on the card.

---

### User Story 2 - Questions come back on a spaced schedule (Priority: P1)

Every answer updates the question's review state with a spaced-repetition algorithm (SM-2 in MVP):
a correct answer pushes the next due date further out (1 day, then 6 days, then interval × ease),
a wrong answer brings the question back tomorrow and resets its streak. The next session only
offers what is due, plus a capped number of new questions, so the daily load stays bounded.

**Why this priority**: Without scheduling the sessions are a quiz, not retention practice.

**Independent Test**: Answer a question correctly three times across simulated days and confirm the
intervals grow 1 → 6 → ~15; answer wrong once and confirm it is due tomorrow with the streak reset;
confirm ease never drops below the floor.

**Acceptance Scenarios**:

1. **Given** a question never answered, **When** answered correctly, **Then** it is due again in 1
   day with repetitions = 1 and ease unchanged.
2. **Given** a question answered correctly once (interval 1 day), **When** answered correctly again,
   **Then** it is due in 6 days with repetitions = 2.
3. **Given** a question with interval 6 and ease 2.5, **When** answered correctly, **Then** it is due
   in 15 days (6 × 2.5 rounded) with repetitions = 3.
4. **Given** any question, **When** answered wrong, **Then** repetitions reset to 0, the interval
   becomes 1 day, ease decreases by the SM-2 amount but never below 1.3, and it is due tomorrow.
5. **Given** a question due today, **When** the next session is built, **Then** it is included
   before any new question.
6. **Given** more due questions than the session limit, **When** the session is built, **Then** the
   most overdue come first and the rest wait for the next session; no new questions are added.

---

### User Story 3 - Choose the scope and size of a session (Priority: P2)

The learner can review everything, one domain, or one topic. They see due/new counts per scope
before starting and can adjust how many questions the session holds (default 20) and how many of
those may be new (default 10).

**Why this priority**: Focused drilling of a weak topic is the most common way to prepare the week
before an interview; the defaults keep the daily load sane.

**Independent Test**: Start a topic-scoped session and confirm only that topic's questions appear;
change the limits and confirm the session size.

**Acceptance Scenarios**:

1. **Given** the learner picks topic `dotnet / clr-memory`, **When** the session starts, **Then**
   every question belongs to that topic and the scope is shown on the card.
2. **Given** limits 10 total / 3 new, **When** the session starts with 4 due and 20 new available,
   **Then** the session holds the 4 due plus 3 new questions.
3. **Given** a scope with nothing due and nothing new, **When** the learner opens Review, **Then**
   they see that there is nothing to review and a suggestion of the earliest upcoming due date.

---

### User Story 4 - See how the session went and where the gaps are (Priority: P2)

When the last question is answered (or the learner ends the session early), a summary shows
answered / correct counts and accuracy, the concepts where answers were wrong ranked by number of
misses, and how many questions are due tomorrow. From the summary the learner can start another
session in the same scope or jump to a weak concept's page (feature 003; until then the link opens
the concept read endpoint's data in place).

**Why this priority**: The summary closes the loop and feeds feature 005 (weak-spots review).

**Independent Test**: Finish a session with a known mix of right/wrong answers and check the
counts, the ranked weak concepts, and the "due tomorrow" number.

**Acceptance Scenarios**:

1. **Given** a finished session with 20 answers, 15 correct, **When** the summary opens, **Then** it
   shows 15 / 20 (75%) and lists concepts with at least one wrong answer, most misses first.
2. **Given** the learner ends a session after 5 of 20 questions, **When** they confirm, **Then** the
   session is finished with those 5 answers and the summary reflects them.
3. **Given** the summary, **When** the learner chooses "Review again", **Then** a new session in the
   same scope starts (containing only what is due or new — questions just answered correctly are
   not repeated).

---

### Edge Cases

- The learner answers the same question twice by double-clicking Check → the second submission is
  ignored (one answer per question per session).
- The page is closed with an open session and reopened days later → the session is still open;
  opening Review resumes it, and questions in it that have meanwhile become due elsewhere are not
  duplicated.
- Content is re-imported while a session is open and a question in it is deprecated → the session
  skips it on resume; its already-recorded answer stays.
- Clock jumps (laptop asleep over midnight) → due-ness is evaluated against the current server time
  when a session is built, never cached in the client.
- A question's options change through re-import mid-session → the client shows what it loaded; the
  answer is stored as the selected option index plus the option text at answer time.
- No content imported at all → Review shows an empty state pointing to the import command.

## Requirements *(mandatory)*

### Functional Requirements

**Session building**

- **FR-001**: The platform MUST build a session for a scope (all / domain / topic) from, in order:
  due questions (review state due at or before now), most overdue first; then new questions (no
  review state), in taxonomy order; limited by a total size (default 20) and a new-question cap
  (default 10), both adjustable per session within 1–100.
- **FR-002**: Only non-deprecated multiple-choice questions whose concept, topic and domain are not
  deprecated are eligible. Unreviewed questions are eligible and flagged.
- **FR-003**: Starting a session MUST finish any open session of the learner first (FR-012 applies
  to it) and MUST record the new session with its scope and limits.
- **FR-004**: The platform MUST report due and new counts for a scope, and the earliest upcoming due
  time when nothing is due, without starting a session.

**Answering**

- **FR-005**: The platform MUST accept exactly one answer per question per session, record it as an
  attempt (selected option index, option text at answer time, correctness, answered-at) linked to
  the session, and reject a second answer for the same question in that session.
- **FR-006**: The answer response MUST include whether it was correct, the index of the correct
  option, the explanation, and the question's next due time, so the client can render feedback
  without a second request.
- **FR-007**: The client MUST show one question at a time, render code in prompts and options as
  code, allow selection by click or keys 1–4, check with a button or Enter, and move on with Next or
  Enter; after checking, the selection is locked.

**Scheduling**

- **FR-008**: Every recorded answer MUST update the question's review state through a scheduling
  interface with SM-2 as the implementation: correct → repetitions + 1, interval 1 → 6 → previous ×
  ease (rounded to whole days, minimum 1), ease unchanged for "good"; wrong → repetitions 0, interval
  1, ease − 0.2 with a floor of 1.3; due = answered-at + interval days; last reviewed = answered-at;
  algorithm-specific state stored alongside.
- **FR-009**: Scheduling math MUST be pure (time passed in) and covered by unit tests for every
  transition listed in User Story 2.
- **FR-010**: Review states MUST be created on first answer and updated in place afterwards; there
  is at most one per learner and question.

**Session lifecycle and summary**

- **FR-011**: The client MUST resume an open session on load, showing the first unanswered question,
  and MUST skip questions that became deprecated.
- **FR-012**: Finishing a session (all answered, or ended early) MUST store finish time, answered
  and correct counts, and MUST return a summary: counts, accuracy, weak concepts (concepts with ≥ 1
  wrong answer in the session, ranked by wrong count then by slug), and the number of questions due
  within the next 24 hours in the same scope.
- **FR-013**: The summary MUST offer "Review again" (new session, same scope) and a link per weak
  concept.

**Presentation**

- **FR-014**: Screens follow ADR 0004: the review card, options, feedback panel, progress bar and
  summary use the design tokens and work in both light and dark themes; the correct option is marked
  with the success color, a wrong selection with the error color.
- **FR-015**: Loading, empty (nothing due), error (API unreachable) and finished states are
  explicit; a failed answer submission can be retried without losing the selection.

### Key Entities *(include if feature involves data)*

- **Session** (existing table): learner, started/finished, scope type + slugs, answered/correct
  counts; gains the ordered list of question ids (membership frozen at start, so resume is
  deterministic) and the two limits used. No existing column changes.
- **Attempt** (existing): item = question, answer payload `{ selectedIndex, selectedText }`,
  `isCorrect`, optional `sessionId`.
- **Review state** (existing): SM-2 fields + `algorithm_state` (`{ lastQuality }`), `algorithm = sm2`.
- **Scope**: all | domain(slug) | topic(domain slug, topic slug).
- **Session summary** (computed): answered, correct, accuracy, weak concepts [{domain, topic,
  concept, title, wrong, total}], dueTomorrow.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A learner can start a session and see the first question within 1 second of pressing
  Start on the local stack.
- **SC-002**: Feedback after Check appears within 300 ms (no full-page reload), and a 20-question
  session can be completed in under 10 minutes of pure interaction.
- **SC-003**: 100% of the SM-2 transitions in User Story 2 are covered by automated tests and behave
  as specified (intervals 1 → 6 → 15 for ease 2.5; wrong resets; ease floor 1.3).
- **SC-004**: After a session, the next session in the same scope contains none of the questions
  just answered correctly and all of the ones answered wrong (they are due the next day) — verified
  by an automated test that advances the clock.
- **SC-005**: Reloading mid-session loses nothing: the same question list and the recorded answers
  are restored.
- **SC-006**: Both themes pass a contrast check on the card states (correct, wrong, neutral).

## Assumptions

- Single local learner (feature 001); no authentication.
- "Now" is the server clock in UTC; the client never computes due-ness.
- Session membership is frozen at start; questions that become due during a session are picked up
  by the next session.
- Session defaults (20 total, 10 new) are constants documented in the environment example; they are
  not persisted per learner in this feature.
- Question order inside a session: due (most overdue first), then new in taxonomy order; no shuffle,
  but the four options keep the order from the seed file (distractor order is authored deliberately).
- The MCQ answer is graded deterministically by comparing the selected index with the correct option
  (constitution Principle VI); no LLM involvement.
- Keyboard shortcuts are desktop conveniences; touch works through the buttons.
- Readiness percentages and the knowledge tree UI are feature 003; the summary links to concept
  pages that 003 will render.
