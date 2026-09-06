# Feature Specification: Foundation — content graph, progress store, import, read API, one-command runtime

**Feature Branch**: `001-foundation`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "Foundation: PostgreSQL schema + EF Core migrations for the content graph (domains, topics, concepts, concept_translations, questions, question_translations, katas, kata_translations) and user progress (users with one seeded local user, attempts, review_states with SM-2 fields plus algorithm_state jsonb, sessions); a content import command (`api import-content`) that validates content/**/*.json against content/schema/content.schema.json and upserts by slug/id; read endpoints for the knowledge tree and a concept page; Docker Compose with postgres, api (FastEndpoints, migrations on startup), web (Angular skeleton served by nginx) with health checks; .env.example. No quiz UI yet."

## Clarifications

### Session 2026-09-06

- Q: What happens to a concept/question that disappeared from every seed file after a re-import? → A: Soft-deprecate: nullable `deprecated_at` on domains, topics, concepts, questions and katas; import marks missing items deprecated and never deletes; tree hides deprecated items by default; progress rows survive.
- Q: Which items get a review state, and how are attempts stored? → A: Questions and katas both; one `review_states` table keyed by (learner, item type, item id) and one `attempts` table with the same discriminator plus answer and grading as opaque structured data.
- Q: Where do domain/topic titles and taxonomy ordering come from? → A: A manifest file `content/manifest.json` (ordered domains with slug + title, each with ordered topics with slug + title); import reads it first and rejects topic files not listed in it; concept order = order inside the topic file; the topic-file schema stays unchanged.
- Q: Must an attempt belong to a session, and what does a session record? → A: Optional link: `attempts.session_id` nullable. A session records learner, start, optional finish, scope (all / domain / topic + slug) and summary counts filled at finish.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bring the platform up with one command (Priority: P1)

The learner clones the repository, copies the example environment file, runs a single start command,
and within a couple of minutes has a database, an API and a web shell running locally, each reporting
healthy. Restarting later keeps all data. Nothing requires manual database setup.

**Why this priority**: Every later feature (review sessions, grading) depends on a runnable local
stack. Without it nothing else can be demonstrated or tested. It also fulfils constitution Principle IX.

**Independent Test**: On a clean machine with only Docker installed, run the documented start command;
verify all three services report healthy and the web shell loads in a browser; stop and start again and
verify previously stored data is still there.

**Acceptance Scenarios**:

1. **Given** a fresh clone and a copied example environment file, **When** the learner runs the single
   start command, **Then** the database, API and web services start, apply any pending schema changes
   automatically, and all report healthy within 2 minutes.
2. **Given** the stack is running, **When** the learner opens the web address, **Then** a placeholder
   page loads and can reach the API (a visible "API: OK" indicator).
3. **Given** the stack was stopped, **When** it is started again, **Then** previously imported content
   and progress are still present.
4. **Given** no LLM API key is configured, **When** the stack starts, **Then** it starts normally and
   nothing in this feature fails or warns loudly (the key is only needed by later features).

---

### User Story 2 - Load curated content from seed files (Priority: P1)

The learner (or the content pipeline) has one or more seed files under the content folder, each
describing a topic with its concepts, notes, questions and katas in the canonical format. Running the
import command validates every file, reports precise errors for invalid ones, and loads valid content
into the platform. Running the import again with unchanged files changes nothing; running it after
editing a note or adding a question updates exactly those items while keeping everything else — in
particular any learning progress already recorded against existing questions.

**Why this priority**: Content is the product (Principle I, II). The import is the only door for content
into the platform and must be trustworthy before any UI is built on top.

**Independent Test**: Import the sample topic file, inspect the loaded knowledge tree through the API;
re-run the import and confirm zero changes; edit one note and one option text, re-run and confirm only
those items changed and the item identifiers stayed the same.

**Acceptance Scenarios**:

1. **Given** a valid seed file, **When** the import runs, **Then** the domain, topic, concepts, notes,
   questions, options, rubrics and katas appear in the platform with the identifiers given in the file,
   and a summary reports counts of created / updated / unchanged items per file.
2. **Given** a seed file that violates the content contract (missing field, five options, two correct
   options, malformed identifier), **When** the import runs, **Then** no part of that file is loaded,
   the error names the file, the path inside the file and the rule broken, and other valid files still
   import (or the whole run is rejected — see FR-013).
3. **Given** content already imported, **When** the same files are imported again, **Then** the summary
   reports everything as unchanged and no stored timestamps of content rows move.
4. **Given** a question whose text was edited in the seed file and a learner already has progress on
   it, **When** the import runs, **Then** the question text updates in place and the learner's progress
   on that question is untouched.
5. **Given** two seed files that reuse the same question identifier, **When** the import runs,
   **Then** the import refuses with an error naming both files.
6. **Given** a seed item with `reviewed: false`, **When** it is imported, **Then** it is loaded and
   marked as unreviewed so that later UI can show a visible "unreviewed" badge; it is never silently
   dropped.
7. **Given** a seed file with a Ukrainian translation for some items, **When** imported, **Then** both
   languages are stored; re-importing an English-only version of the same file leaves the existing
   Ukrainian text in place (translations are added or updated per language, never wiped by omission).

---

### User Story 3 - Browse the knowledge tree and read a concept (Priority: P2)

The learner (through the API for now, through the web UI in feature 003) can list all domains with
their topics and concepts — with each concept's level and whether it is reviewed — and open a single
concept to read its note, see how many questions and katas it has, and switch between English and
Ukrainian where a translation exists.

**Why this priority**: These are the two read paths every later screen builds on (tree navigation,
concept page). Delivering them now lets feature 003 be a pure UI feature.

**Independent Test**: After importing the sample topic, request the tree and confirm it mirrors the
seed file's structure and ordering; request one concept in each language and confirm the note and
counts.

**Acceptance Scenarios**:

1. **Given** imported content, **When** the learner requests the knowledge tree, **Then** every domain,
   topic and concept is returned in the taxonomy order with slug, title, level, reviewed flag and per
   concept question/kata counts.
2. **Given** a concept slug and a language, **When** the learner requests the concept page, **Then**
   the title and note in that language are returned, or in English with a "fallback" marker when the
   requested language has no translation.
3. **Given** an unknown concept slug, **When** requested, **Then** a clear not-found response is
   returned.

---

### User Story 4 - Progress is recorded per learner from day one (Priority: P3)

Even though no quiz exists yet, the platform already has a single local learner and the storage for
answers, review schedule state and sessions, so that the next feature can record a review session
without changing the data model. The seeded learner exists after the first start and is the implicit
owner of all progress.

**Why this priority**: Principle IV requires user-scoped progress with a single seeded user; doing it
in the foundation avoids a migration later. It has no user-visible behaviour yet, hence P3.

**Independent Test**: After first start, verify exactly one learner exists; verify the progress
structures exist and are empty; verify content rows carry no learner reference.

**Acceptance Scenarios**:

1. **Given** a fresh database, **When** the API starts for the first time, **Then** exactly one
   learner record exists with a stable identifier and a preferred language of English.
2. **Given** the API is restarted, **When** it starts again, **Then** still exactly one learner exists
   (seeding is idempotent).

---

### Edge Cases

- Seed folder is empty or missing → import reports "nothing to import" and exits successfully.
- A concept, question or kata present in the database is no longer present in any seed file (or a
  domain/topic is no longer in the manifest) → it is marked deprecated with a timestamp, never deleted;
  its progress rows survive; the knowledge tree hides it unless explicitly asked to include deprecated
  items. Re-adding it to a seed file clears the deprecation.
- A topic file names a domain or topic that is not in `content/manifest.json` → the import rejects the
  run naming the file and the missing manifest entry.
- A question changes type (mcq → open) between imports for the same id → import rejects the file
  (type is part of identity).
- A note exceeds any reasonable size (e.g. > 50 KB) → accepted; no truncation in this feature.
- Two import commands run at the same time → the second one fails fast with a "import already in
  progress" message rather than corrupting data.
- The database is unreachable at API start → the API retries for a bounded time (at least 60 s) and
  then exits with a non-zero code so the orchestrator restarts it.
- Migrations fail at start → the API logs the failure and exits non-zero; it never serves requests on
  a half-migrated schema.
- Requesting the tree before any import → an empty tree, not an error.

## Requirements *(mandatory)*

### Functional Requirements

**Runtime**

- **FR-001**: The platform MUST start with one documented command that brings up the database, the
  API and the web shell, and each service MUST expose a health check the orchestrator uses to order
  start-up (API waits for a healthy database; web waits for a healthy API).
- **FR-002**: The API MUST apply pending schema changes automatically at start-up and MUST refuse to
  serve requests if that fails.
- **FR-003**: All configuration MUST come from environment variables; an example environment file MUST
  list every variable with a comment, and the real environment file MUST be ignored by version control
  and never written by tooling.
- **FR-004**: Data MUST persist across restarts of the stack.
- **FR-005**: The web shell MUST be served as static files by a web server that also forwards API
  calls under a single origin, and MUST show whether the API is reachable.

**Content model**

- **FR-006**: The platform MUST store the content graph as domains → topics → concepts, with each
  concept having translated title and note per language, a level (M/S/L), a reviewed flag and an
  ordering that matches the taxonomy.
- **FR-007**: The platform MUST store questions (type mcq or open) with per-language prompt,
  explanation, options (mcq: exactly four, exactly one correct) or rubric (open: three to five
  criteria), plus level, reviewed flag and tags; and katas with per-language task, rubric and
  reference solution, plus language, reviewed flag and time limit.
- **FR-008**: English MUST be mandatory for every translatable item; Ukrainian MUST be optional per
  item (Principle III).
- **FR-009**: Every slug (domain, topic, concept) and every question/kata identifier MUST be unique
  and stable; the platform MUST never regenerate or reassign them.

**Import**

- **FR-010**: The import command MUST validate every seed file against the content contract file
  (`content/schema/content.schema.json`) before touching the database and MUST report the file, the
  location inside the file and the violated rule for every error.
- **FR-011**: The import MUST be an idempotent upsert keyed by slug (domain, topic, concept) and by
  identifier (question, kata), with translations upserted per (item, language). Re-importing unchanged
  files MUST result in zero changes and MUST NOT bump any "updated" timestamps.
- **FR-012**: The import MUST never modify or delete learner progress (attempts, review states,
  sessions), including when the question it refers to is updated.
- **FR-013**: The import MUST run as a single all-or-nothing operation across all files given: if any
  file is invalid or any identifier is duplicated across files, nothing is written.
- **FR-014**: The import MUST print a summary per file (created / updated / unchanged counts per entity
  type) and exit with a non-zero code on failure.
- **FR-015**: The import MUST be runnable both from the developer machine and inside the running stack
  (documented command for each).
- **FR-016**: Unreviewed items MUST be imported and flagged, never dropped (constitution Development
  Workflow: "Unreviewed content is imported with a visible 'unreviewed' badge").

**Read access**

- **FR-017**: The platform MUST expose a knowledge-tree read that returns all domains, topics and
  concepts in taxonomy order with slug, title (requested language with English fallback), level,
  reviewed flag and counts of questions and katas per concept.
- **FR-018**: The platform MUST expose a concept read by slug and language returning title, note,
  level, reviewed flag, question and kata counts, and an explicit indicator when the note fell back to
  English.
- **FR-019**: Read responses MUST NOT include learner-specific data (this feature has none to show;
  the separation is a design rule from Principle IV).

**Learner & progress storage**

- **FR-020**: The platform MUST seed exactly one local learner on first start, idempotently, with a
  stable identifier and preferred language.
- **FR-021**: The platform MUST provide storage for attempts (which learner answered which question or
  kata, when, with what answer, and the outcome/grade), review states (per learner and per scheduled
  item: interval in days, ease factor, repetition count, last reviewed time, next due time, and an
  opaque algorithm-specific state blob) and sessions (a review sitting with start/end and scope). All
  three MUST reference the learner; no content entity may reference a learner.
- **FR-022**: Review states MUST exist for both questions and katas, keyed by (learner, item type,
  item identifier) with at most one state per key; attempts MUST be recorded in a single store with the
  same item discriminator, an opaque answer payload, an opaque grading payload and an optional link to a
  session.
- **FR-023**: A session MUST record the learner, start time, optional finish time, scope (all / one
  domain / one topic, by slug) and summary counts (answered, correct) written when it finishes.

**Deprecation**

- **FR-024**: Every content entity (domain, topic, concept, question, kata) MUST carry an optional
  "deprecated at" timestamp. The import MUST set it on items that are absent from the current seed set
  (or, for domains/topics, from the manifest) and MUST clear it when the item reappears. The import MUST
  never delete content rows.
- **FR-025**: The knowledge-tree and concept reads MUST exclude deprecated items by default and MUST
  offer an explicit option to include them.

**Manifest**

- **FR-026**: The content set MUST include `content/manifest.json` listing domains in order with slug
  and title, each with its topics in order with slug and title, validated against a committed contract
  file. The import MUST read it first, MUST reject any topic file whose domain/topic pair is not listed,
  and MUST take domain/topic titles and order from it. Concept order is the order inside the topic file.

### Key Entities *(include if feature involves data)*

- **Domain**: top-level area (`dotnet`, `angular`, …). Slug, title, order (from the manifest),
  deprecated-at.
- **Topic**: belongs to one domain. Slug (unique within the domain), title, order (from the manifest),
  deprecated-at.
- **Concept**: belongs to one topic. Slug (unique within the topic), level (M/S/L), reviewed flag,
  order (position in the topic file), deprecated-at; has translations (language → title, note),
  questions and katas.
- **Concept translation**: one per (concept, language). Title, note (Markdown).
- **Question**: belongs to one concept. Identifier `<domain>.<topic>.<concept>.<type>.<n>`, type
  (mcq/open), level, reviewed flag, tags, deprecated-at; has translations.
- **Question translation**: one per (question, language). Prompt, explanation, options (mcq) or rubric
  (open).
- **Kata**: belongs to one concept. Identifier `<domain>.<topic>.<concept>.kata.<n>`, programming
  language, reviewed flag, time limit, deprecated-at; has translations.
- **Manifest**: `content/manifest.json` — ordered domains (slug, title) with ordered topics (slug,
  title). The source of domain/topic titles and ordering.
- **Kata translation**: one per (kata, language). Task, rubric, reference solution.
- **Learner (user)**: the single local learner. Stable identifier, display name, preferred language.
- **Attempt**: a learner's answer to one item (item type: question or kata; item identifier) at a
  point in time, with the given answer (opaque structured payload), the outcome (correct / score) and
  grading details (opaque structured payload); optionally linked to a session.
- **Review state**: the learner's spaced-repetition state for one item (item type: question or kata;
  item identifier), at most one per (learner, item): interval in days, ease, repetitions, last reviewed,
  due, algorithm-specific state (opaque).
- **Session**: a review sitting: learner, start, optional finish, scope type (all / domain / topic) and
  scope slug, summary counts (answered, correct) filled at finish.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time start on a machine with only the container runtime installed reaches "all
  services healthy" in under 2 minutes, with no manual steps beyond copying the example environment
  file.
- **SC-002**: Importing the full MVP content set (5 domains, roughly 20 topics, up to ~600 questions)
  completes in under 30 seconds, and a repeat import of unchanged files reports 0 created / 0 updated.
- **SC-003**: 100% of contract violations in a seed file are reported with file, location and rule;
  an invalid file never results in partially loaded content.
- **SC-004**: Updating a question's text and re-importing leaves 100% of existing progress rows for
  that question intact and unchanged.
- **SC-005**: Knowledge-tree and concept reads for the full MVP content set return in under 300 ms on
  a laptop.
- **SC-006**: The data model supports features 002–005 (review session, notes UI, LLM grading, weak
  spots) without renaming or removing any table or column introduced here (verified when those
  features are planned).

## Assumptions

- Single local learner; no authentication (constitution Non-Goals). The learner identifier is fixed and
  documented so later features and tests can rely on it.
- The content contract is `content/schema/content.schema.json` as committed; this feature does not
  change it. Ordering inside a seed file is the taxonomy ordering.
- `content/manifest.json` mirrors the numbering of `specs/000-taxonomy.md`; keeping the two in sync
  is a content-authoring duty (the `content-author` agent reads both).
- Health checks are simple liveness/readiness endpoints; no metrics or tracing in this feature.
- The web shell is an empty application skeleton with the project's front-end conventions applied and
  one status indicator; all real screens come in features 002–005.
- The import stores a content hash per item to detect "unchanged" cheaply; this is an implementation
  convenience, not a contract.
- Attempts store the raw answer and grading details as opaque structured data so that feature 004 can
  add LLM grading output without a schema change.
- No data retention limits: attempts and sessions are kept indefinitely (single user, local).
- Ukrainian text is stored as provided; no automatic translation in this feature.
