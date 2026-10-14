# 6. Atlas visual direction, runtime EN/UK UI and the name "Spine"

- **Status:** Accepted (supersedes the visual part of ADR 0004)
- **Date:** 2026-10-15

## Context

After feature 002 the user found the "Calm workspace / Developer dark" look dated and asked for a
redesign that makes the resource attractive, for a Ukrainian UI next to English, and for a more
serious name than "InterviewPrep". Three directions (Signal, Atlas, Deck) and three navigation
variants for Atlas (spine rail, floating dock, masthead with thumb index) were drawn on the design
canvas "InterviewPrep Redesign". The user chose Atlas with the spine rail, and the name Spine.

## Decision

**Visual direction "Atlas".** Light ground `#eef1f7`, white surfaces, ink panels `#0f1630` for the
rail, the Today card and answer feedback; accent cobalt `#2e5bff`; five domain colors by manifest
order (cobalt, teal, tangerine, violet, raspberry). Type: Unbounded (display), Golos Text (body),
JetBrains Mono — all three cover Cyrillic. Answer states: blue = right, orange = wrong (they also
differ in lightness) plus ✓/✗ marks. A dark Atlas variant keeps the same structure with a deep navy
ground. Tokens live in `web/src/styles.scss` and are exposed as Tailwind utilities (`bg-ink`,
`text-on-ink`, `bg-ok-fill`, `domain-1…5`). Every text pair was measured ≥ 4.5:1 in both themes.

**Navigation "spine rail".** A vertical ink rail (horizontal bar on narrow screens) with the mark,
sections, domain tabs 01–05 that open Review scoped to the domain, language, theme and API status.

**Runtime i18n, no library.** An `I18n` root service holds `lang` as a signal; dictionaries are typed
objects (`messages.en.ts` defines the type, `messages.uk.ts` must satisfy it); plurals use
`Intl.PluralRules`, dates and percentages `Intl`. Rejected: Angular `$localize` (one build per
locale, reload on switch, extra nginx routing) and third-party libraries (constitution VIII spirit:
no extra state machinery for a two-language single-user app). The API receives `?lang=uk` only when
the language is not English, so English URLs stay unchanged.

**Name "Spine — the senior engineer's interview atlas".** Solution, projects and namespaces become
`Spine.*`. The PostgreSQL database and role keep `interviewprep`: renaming them would orphan the
user's data volume and requires editing the user-maintained `.env`.

## Consequences

- New screens use the Atlas tokens; raw palette classes stay forbidden.
- Content language is per item: a question is Ukrainian only when its Ukrainian options align with
  the English ones; otherwise the whole question falls back, flagged `fallback: true`.
- Domain and topic titles remain English until the manifest carries `uk` titles (follow-up).
- The canvas "InterviewPrep Redesign" is the visual reference; the old ADR 0004 canvas is history.
