# Feature Specification: Activity and streak

**Feature Branch**: `007-activity-streak`

**Created**: 2026-11-03

**Status**: Draft

**Input**: User description: "Activity and streak card on the review overview. The learner sees: the
current streak (consecutive calendar days, up to today, with at least one answered question; today
without answers yet does not break a streak that reached yesterday), the longest streak ever, and the
number of answers per day for the last 14 days as a small bar chart. Data comes from existing attempts,
no schema change. Day boundaries follow the learner's time zone: the web app sends its time zone and
unknown zones are rejected; without it the day is UTC. EN and UK, light and dark, accessible chart.
Out of scope: goals, notifications, per-domain streaks, katas (only answered questions count, any
correctness)."

## Clarifications

### Session 2026-11-03

- Q: Whose calendar defines a "day"? → A: The learner's time zone as reported by their browser; when none
  is given, UTC. An unknown time zone is an error, not a silent fallback.
- Q: Does a day with only wrong answers count? → A: Yes. Any answered question counts, whatever its
  correctness, session mode or scope.
- Q: Does an answer to a question that was deprecated later still count? → A: Yes. Activity is history:
  it reflects what the learner did on that day.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Keep my streak going (Priority: P1)

On the review overview the learner sees how many days in a row they have answered at least one question,
and their longest run ever. If they have not answered anything today yet but did yesterday, the streak
still shows yesterday's run, with a hint that answering today keeps it going.

**Why this priority**: A visible streak is the simplest daily nudge for spaced repetition, which only
works when reviews happen every day.

**Independent Test**: Seed answers on known local dates, open the overview (or ask the activity
endpoint) and compare the current and longest streak with a hand count.

**Acceptance Scenarios**:

1. **Given** answers on each of the last 3 days including today, **When** the overview opens, **Then** the current streak is 3.
2. **Given** answers yesterday and the 2 days before but none today, **When** the overview opens, **Then** the current streak is 3 and the card says that answering today extends it.
3. **Given** the last answer was 2 days ago, **When** the overview opens, **Then** the current streak is 0.
4. **Given** an earlier run of 5 days and a current run of 2, **When** the overview opens, **Then** the longest streak is 5.
5. **Given** an answer at 23:30 and another at 00:30 local time on the next day, **When** the streak is computed in the learner's time zone, **Then** they fall on two consecutive days although both are on the same UTC day.
6. **Given** no answers at all, **When** the overview opens, **Then** both streaks are 0 and the card invites the learner to start a review.

---

### User Story 2 - See my last two weeks (Priority: P2)

Next to the streak, a small bar chart shows how many questions the learner answered on each of the last
14 days, today included, with empty days shown as gaps. The same figures are available as text for screen
readers.

**Why this priority**: The streak says whether the habit holds; the two-week view shows how much work
went in and where the gaps are.

**Independent Test**: Seed answers on known local dates within and outside the 14-day window; compare the
14 daily counts with a hand count.

**Acceptance Scenarios**:

1. **Given** answers on some of the last 14 days, **When** the overview opens, **Then** the chart has exactly 14 days, oldest first, ending today, with zero for days without answers.
2. **Given** answers older than 14 days, **When** the chart is built, **Then** they are not shown but still count for the longest streak.
3. **Given** a screen reader, **When** the learner reaches the chart, **Then** a text alternative lists the total for the period and the count per day.
4. **Given** the learner switches language or theme, **When** the card re-renders, **Then** figures stay the same and labels, weekdays and plurals follow the selected language.

---

### Edge Cases

- A time-zone change (travel or daylight saving) only moves where day boundaries fall; each answer is
  assigned to the calendar date it has in the requested time zone.
- An answer recorded a few seconds "in the future" because of clock skew counts for the day it falls on;
  no day after today is ever shown.
- Several answers to the same question on one day count as several answers for the chart and as one
  active day for the streak.
- The activity card fails to load: the rest of the overview still works and the card shows a short
  error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST assign every answer to a calendar day in the time zone the client requests, MUST use UTC when no time zone is given, and MUST reject an unknown time zone with a clear error.
- **FR-002**: The system MUST report the current streak: the number of consecutive days with at least one answer ending today, or ending yesterday when today has no answer yet, and 0 otherwise; it MUST also report whether today already has an answer.
- **FR-003**: The system MUST report the longest streak over the learner's whole answer history.
- **FR-004**: The system MUST report the number of answers on each of the last 14 days, today included, oldest first, with 0 for days without answers.
- **FR-005**: Activity MUST count every answer to a question by the single local learner, whatever its correctness, session mode, scope, language or later deprecation, and MUST NOT count katas.
- **FR-006**: The review overview MUST show a card with the current streak, the longest streak, a hint when today has no answer yet but the streak is alive, and the 14-day chart, using the Atlas tokens in both themes.
- **FR-007**: The web app MUST request activity in the browser's time zone.
- **FR-008**: The chart MUST have a text alternative with the period total and the count for each day, and all new copy MUST exist in EN and UK with correct plurals.
- **FR-009**: With no answers the card MUST show an empty state inviting the learner to start a review; when activity cannot be loaded the card MUST show an error while the rest of the overview keeps working.

### Key Entities

- **Answer (attempt)**: existing record of each answer with its time; the only source of activity.
- **Activity**: derived per request — current streak, whether today is active, longest streak, and 14
  daily counts in the requested time zone. Nothing new is stored.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a seeded history, the current streak, longest streak and the 14 daily counts match a
  hand count in 100% of checked cases, in at least two different time zones.
- **SC-002**: The activity card is visible within 1 second of opening the overview on the local stack with
  a year of daily reviews.
- **SC-003**: The card passes a keyboard and screen-reader walkthrough and causes no horizontal scroll at
  375 px in both languages and themes.

## Assumptions

- Only multiple-choice answers exist until LLM grading (005); open-question answers will count
  automatically once they are recorded as answers to questions.
- The 14-day window is fixed, not configurable; no goals, reminders or notifications.
- The browser's time zone is trusted as the learner's; there is no stored time-zone preference.
- Single local learner; no authentication; no schema change.
