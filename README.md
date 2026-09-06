# InterviewPrep

A single-user, locally-run interview-preparation platform for Senior Full-stack (Angular + .NET + AI
Engineering): knowledge graph → spaced-repetition quizzes (SM-2) → LLM-graded open answers and katas.
Content is data (`content/**/*.json`), authored with AI, reviewed by a human.

- Principles: `.specify/memory/constitution.md`
- Content scope: `specs/000-taxonomy.md`
- Working with Claude Code here: `CLAUDE.md`
- Stack reference: `docs/architecture/STACK.md`

## Run (after feature 001)

```bash
cp .env.example .env            # fill ANTHROPIC_API_KEY (optional — MCQ/notes work without it)
docker compose up --build       # postgres + api (http://localhost:8080) + web (http://localhost)
docker compose run --rm api import-content
```
