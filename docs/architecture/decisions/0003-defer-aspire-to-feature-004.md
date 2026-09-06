# 3. Defer Aspire (AppHost + ServiceDefaults) to feature 004

- **Status:** Accepted
- **Date:** 2026-09-06

## Context

Mid-way through feature 001 the question came up whether to adopt Aspire (formerly .NET Aspire;
13.5.x as of 2026-08, .NET 10 only) now, "before it is too late". A research pass (deep-researcher,
2026-09-06; brief kept in the personal `working/` folder, sources on aspire.dev and the
`microsoft/aspire` repository) established:

- Aspire is explicitly designed to be added to an existing app (`aspire init`); our seams
  (`AddInfrastructure` / `ConfigureDbContext`, `ConnectionStrings__Default`, `/health/ready`) already
  match what it injects. Nothing gets harder by waiting.
- `aspire publish` with the Docker Compose publisher emits `image:` (not `build:`), `depends_on` with
  `service_started` only and **no `healthcheck`** blocks (issues #8208 and #9194 open). The generated
  compose would be worse than the committed one, so the AppHost cannot be the single source of truth
  for Principle IX ("one command to run") yet; both would have to coexist.
- Real benefits for this project — dashboard with traces/logs/metrics, `aspire run` inner loop,
  Claude Code MCP visibility into resource state — mostly pay off once there are external calls to
  trace (feature 004, Anthropic grading). Costs: two more projects in a deliberately thin solution, a
  second run path to keep in sync with compose, quarterly releases with breaking changes, Node/nvm
  PATH quirks for `ng serve` under the AppHost.

## Decision

Do not adopt Aspire in 001–003. Plan its adoption as part of **feature 004 (LLM grading)**: AppHost
(Postgres 16 with data volume + pgweb, Api with `WaitFor`, Angular via `AddJavaScriptApp`,
`import-content` as an explicit-start resource), ServiceDefaults referenced by Api, `aspire agent init`
for the MCP entry, and a constitution amendment to Principle IX stating that `docker compose up` stays
the one-command runtime while `aspire run` is the optional inner loop. The hand-written
`docker-compose.yml` stays until `aspire publish` emits health checks and `build:`.

Alternative kept in reserve: the standalone Aspire dashboard container + OTel exporter in Api
(1–2 hours) if traces are wanted before 004.

## Consequences

- 001 finishes on the working compose setup; no rework expected when Aspire lands.
- Feature 004's spec must include the Aspire tasks and the constitution bump (MINOR).
- Re-evaluate immediately if a second backend process appears (worker, Redis, LLM proxy) or if
  the compose publisher gains health checks — then adoption is a one-day job.
