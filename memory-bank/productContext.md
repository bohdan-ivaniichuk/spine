# Product Context — Spine

## The user

One person: a senior full-stack engineer (Angular + .NET) preparing for Senior / Lead interviews and
adding practical AI-engineering depth. Reviews in short daily sessions (15–30 min), mostly on a laptop,
sometimes wants Ukrainian explanations for faster reading but must answer in English terminology.

## Jobs to be done

| Job | How the product serves it |
|---|---|
| "What should I review today?" | Due queue from SM-2 plus a capped number of new questions, filterable by domain/topic. |
| "Do I actually understand this?" | MCQs with plausible-misconception distractors and explanations that say why each wrong option is wrong. |
| "Can I explain it out loud?" | Open questions with a 3–5 bullet rubric, graded by an LLM into `{score, met, missed, feedback}`. |
| "Can I still write the code?" | Katas in Monaco, graded against a rubric and a reference solution. |
| "Where am I weak?" | Session summary with weak concepts; per-domain readiness = share of concepts with ease ≥ 2.5 and interval ≥ 7 days. |
| "Give me the material" | Concept notes (300–900 words) with code samples, `en`/`uk`. |

## Content model

Taxonomy (`specs/000-taxonomy.md`): 5 MVP domains — `dotnet`, `aspnet-data`, `angular`,
`architecture`, `ai-engineering`. Each concept carries a level `M`/`S`/`L`. Content is authored by the
`content-author` subagent, reviewed by the user (flip `reviewed: true`), then imported. Unreviewed
content still imports but is badged.

## Feature roadmap (from `working/KICKOFF_PROMPTS.md`, personal)

1. **001 Foundation** — schema, migrations, import command, read endpoints, Docker Compose.
2. **002 Review session** — MCQ + SM-2.
3. **003 Knowledge tree & notes** — browse, markdown, language switch, readiness.
4. **004 LLM grading** — open + kata, Monaco, structured output, SSE feedback; **plus Aspire adoption** (AppHost, ServiceDefaults, dashboard) per ADR 0003.
5. **005 Weak-spots review** — personalised review of failed questions, "drill this topic".
