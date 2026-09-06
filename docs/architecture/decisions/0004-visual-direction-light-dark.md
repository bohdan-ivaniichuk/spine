# 4. Visual direction: "Calm workspace" light theme + "Developer dark" dark theme

- **Status:** Accepted
- **Date:** 2026-09-06

## Context

Feature 001 shipped a bare Angular shell with default Tailwind styling. Before the first real
screens (002 review session, 003 knowledge tree and notes) the user reviewed four deliberately
different directions on a design canvas, each drawn on the two key screens (daily review MCQ card;
concept page with the knowledge tree): A "Calm workspace", B "Developer dark", C "Editorial paper",
D "Focused flashcards". The user chose A and B together, as the light and dark themes of one product.

## Decision

- **One typography, two palettes.** Manrope for UI and headings, JetBrains Mono for code, ids and
  badges — in both themes. Only colors, card radius and shadow switch.
- **Tokens as CSS variables** in `web/src/styles.scss`, exposed to Tailwind 4 through `@theme inline`
  (`bg-bg`, `bg-surface`, `text-muted`, `border-line`, `bg-accent`, `bg-ok-soft`, `font-mono`, …).
  Light values on `:root`; dark values under `@media (prefers-color-scheme: dark)` guarded by
  `:root:not([data-theme='light'])` and again under `:root[data-theme='dark']`, so an explicit
  choice wins in both directions.
- **Theme service** (`web/src/app/shared/theme/theme.ts`): `preference` signal
  (`system | light | dark`), persisted in `localStorage`, applied as `data-theme` on `<html>`;
  `system` removes the attribute. A header toggle switches between the two explicit themes.
- **Design canvas** (Claude Design artifact "InterviewPrep Design Directions") keeps the chosen
  screens on page one and the unchosen sketches on page two; C's reading rhythm may still inform
  the note page (`.prose-note`), D's two-column options may return for the kata screen.

Palette (light → dark): bg `#f7f7f5` → `#0f141a`; surface `#ffffff` → `#161c24`; text `#1f2328` →
`#e6edf3`; muted `#6b7280` → `#8b98a8`; line `#e5e5e1` → `#2a3441`; accent `#3b6cf0` → `#7dd3fc`;
ok `#1f9d61` → `#7ee787`; bad `#d6453d` → `#ff7b72`; code bg `#f3f4f6` → `#0b0f14`; card radius
10px → 6px.

## Consequences

- Features 002–005 use the token utilities only; no raw Tailwind palette colors (`slate-*`,
  `sky-*`) in components. `frontend-reviewer` flags raw palette classes.
- Markdown notes render inside `.prose-note`; syntax highlighting maps to the `--color-syntax-*`
  tokens.
- Monaco (feature 004) gets a light and a dark editor theme derived from the same tokens.
