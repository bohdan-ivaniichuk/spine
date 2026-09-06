# Data model — 001 Foundation

PostgreSQL 16, EF Core 10, snake_case via `EFCore.NamingConventions`. All timestamps `timestamptz`
(UTC). `created_at`/`updated_at` `NOT NULL` on every table (except `attempts`/`users`/`sessions` which
have only `created_at` where noted). `deprecated_at timestamptz NULL` on the five content entities.
Enums are `text` + CHECK. Content tables never reference `users`.

## Content graph

| Table | Columns | Keys / constraints / indexes |
|---|---|---|
| `domains` | `id int identity`, `slug text`, `title text`, `sort_order int`, `content_hash char(64)`, `deprecated_at`, `created_at`, `updated_at` | PK `id`; UNIQUE `slug` |
| `topics` | `id int identity`, `domain_id int`, `slug`, `title`, `sort_order`, `content_hash`, `deprecated_at`, timestamps | PK; FK `domain_id → domains` RESTRICT; UNIQUE `(domain_id, slug)` |
| `concepts` | `id int identity`, `topic_id int`, `slug`, `level text` CHECK IN ('M','S','L'), `reviewed bool` (derived at import: all non-deprecated questions+katas reviewed), `sort_order`, `content_hash`, `deprecated_at`, timestamps | PK; FK `topic_id → topics` RESTRICT; UNIQUE `(topic_id, slug)` |
| `concept_translations` | `concept_id int`, `language text` CHECK IN ('en','uk'), `title text`, `note text`, `content_hash`, timestamps | PK `(concept_id, language)`; FK cascade |
| `questions` | `id text` (content id, CHECK matches `^[a-z0-9-]+\.[a-z0-9-]+\.[a-z0-9-]+\.(mcq|open)\.[0-9]+$`), `concept_id int`, `type text` CHECK IN ('mcq','open'), `level`, `reviewed bool`, `tags text[]`, `sort_order`, `content_hash`, `deprecated_at`, timestamps | PK `id`; FK `concept_id → concepts` RESTRICT; INDEX `(concept_id)` |
| `question_translations` | `question_id text`, `language`, `prompt text`, `explanation text`, `options jsonb NULL` (`[{text,correct}]×4`), `rubric jsonb NULL` (`string[]`), `content_hash`, timestamps | PK `(question_id, language)`; CHECK `(options IS NULL) <> (rubric IS NULL)` |
| `katas` | `id text` PK (CHECK `…\.kata\.[0-9]+$`), `concept_id int`, `title text`, `language text` CHECK IN ('csharp','typescript','sql'), `reviewed bool`, `time_limit_minutes int` CHECK 5..120, `sort_order`, `content_hash`, `deprecated_at`, timestamps | FK `concept_id` RESTRICT; INDEX `(concept_id)` |
| `kata_translations` | `kata_id text`, `language`, `task text`, `rubric jsonb` (`string[]`), `reference_solution text`, `content_hash`, timestamps | PK `(kata_id, language)` |

Ordering: domains/topics from `content/manifest.json` index; concepts/questions/katas from their
position in the topic file.

## Learner and progress

| Table | Columns | Keys / constraints / indexes |
|---|---|---|
| `users` | `id uuid` PK, `display_name text`, `preferred_language text` CHECK IN ('en','uk'), `created_at` | seeded row `00000000-0000-0000-0000-000000000001` (`LocalLearner.Id`) |
| `review_states` | `id uuid` PK (v7, app-generated), `user_id uuid`, `item_type text` CHECK IN ('question','kata'), `question_id text NULL`, `kata_id text NULL`, `item_id text GENERATED ALWAYS AS (COALESCE(question_id, kata_id)) STORED`, `algorithm text NOT NULL DEFAULT 'sm2'`, `interval_days int NOT NULL DEFAULT 0`, `ease double precision NOT NULL DEFAULT 2.5`, `repetitions int NOT NULL DEFAULT 0`, `last_reviewed_at timestamptz NULL`, `due_at timestamptz NOT NULL`, `algorithm_state jsonb NULL`, `created_at`, `updated_at` | FK `user_id → users`; FK `question_id → questions` RESTRICT; FK `kata_id → katas` RESTRICT; CHECK `(item_type='question' AND question_id IS NOT NULL AND kata_id IS NULL) OR (item_type='kata' AND kata_id IS NOT NULL AND question_id IS NULL)`; UNIQUE `(user_id, item_id)`; INDEX `(user_id, due_at)` |
| `attempts` | `id uuid` PK, `user_id`, `item_type`, `question_id NULL`, `kata_id NULL`, `item_id` GENERATED, `session_id uuid NULL`, `answered_at timestamptz`, `answer jsonb NOT NULL`, `is_correct bool NULL`, `score double precision NULL` CHECK 0..1, `grading jsonb NULL`, `created_at` | FKs as above; FK `session_id → sessions` SET NULL; same item CHECK; INDEX `(user_id, item_id, answered_at DESC)`; INDEX `(session_id)` |
| `sessions` | `id uuid` PK, `user_id`, `started_at`, `finished_at NULL`, `scope_type text` CHECK IN ('all','domain','topic'), `scope_domain_slug text NULL`, `scope_topic_slug text NULL`, `answered_count int NULL`, `correct_count int NULL`, `created_at` | FK `user_id`; CHECK scope consistency (`all` → both NULL; `domain` → domain set, topic NULL; `topic` → both set); INDEX `(user_id, started_at DESC)` |

## Validation rules (import, beyond JSON Schema) — rule ids

`manifest-pair`, `file-path`, `duplicate-topic-file`, `concept-slug-unique`, `id-prefix`, `id-type`,
`mcq-one-correct`, `concept-title`, `duplicate-id`, `manifest-duplicates`. Each error is reported as
`(file, jsonPointer, rule, message)`.

## State transitions

- Content entity: *absent* → **created** (hash stored) → **unchanged** (same hash, not deprecated:
  entity not touched) | **updated** (hash differs or was deprecated: fields set, `updated_at = now`,
  `deprecated_at = NULL`) → **deprecated** (absent from the current seed set / manifest:
  `deprecated_at = now`) → **reactivated** (reappears: counted as updated).
- Translation: per `(entity, language)`: created / updated / unchanged; a language absent from the file
  is left untouched.
- Existing question whose `type` differs from the incoming one → validation error (a type change is a
  new id by construction).
- A topic listed in the manifest whose seed file is absent: its concepts (and their questions/katas) are
  deprecated like any other absent item (FR-024). Re-adding the file reactivates them.
- Progress rows: never created, updated or deleted by import.
- Indexes: `(user_id, due_at)` and unique `(user_id, item_id)` on `review_states`; no standalone `due_at`
  index on purpose — every due-queue query is per learner (Principle IV).

## EF mapping notes

- `options`/`rubric`: `HasColumnType("jsonb")` + value converter to `IReadOnlyList<McqOption>` /
  `IReadOnlyList<string>` **with a ValueComparer**; `tags` as native `string[]`.
- `item_id`: `HasComputedColumnSql("COALESCE(question_id, kata_id)", stored: true)`.
- CHECKs via `ToTable(t => t.HasCheckConstraint(...))`.
- No navigation from content entities to `users`.
