# 000 — Knowledge Taxonomy (MVP)

Consumed by the `content-author` subagent. Structure: **Domain → Topic → Concept**. Each concept carries a target level: `M` (mid), `S` (senior), `L` (lead). Slugs are stable identifiers used in seed files; never rename a slug, deprecate instead.

Per concept the content pipeline produces:
- 1 `note` (EN, 300–900 words, with a code sample when the concept is code-bound), later `uk` translation
- 3–6 `mcq` questions (4 options, exactly one correct, distractors must be plausible misconceptions)
- 1–2 `open` questions with a grading rubric (3–5 bullet criteria)
- 0–1 `kata` (only where the taxonomy marks `[kata]`)

Priority order for the first content pass: domain 1 → 2 → 3 → 4 → 5. Within a domain, `S` concepts first.

---

## Domain 1 — `dotnet` · C# & .NET Runtime

### 1.1 `clr-memory` · Memory & GC
- `stack-vs-heap` (M) — value/reference semantics, boxing, when structs live on the heap
- `gc-generations` (S) — gen0/1/2, LOH, POH, why generational works, promotion
- `gc-modes` (S) — workstation vs server, concurrent/background GC, `GCSettings`, container limits
- `finalization-dispose` (S) — `IDisposable`, `IAsyncDisposable`, finalizers, `SafeHandle`, dispose pattern
- `memory-leaks-managed` (S) — event handler leaks, static caches, closures, timers, `WeakReference`
- `span-memory` (S) — `Span<T>`, `Memory<T>`, `stackalloc`, `ArrayPool`, zero-allocation parsing [kata]
- `string-internals` (M) — immutability, interning, `StringBuilder`, `string.Create`, UTF-16

### 1.2 `async` · Async & Threading
- `async-await-internals` (S) — state machine, `Task` vs `ValueTask`, continuation, `ConfigureAwait`
- `sync-context` (S) — why deadlocks happened in ASP.NET classic and why not in Core
- `task-vs-thread` (M) — thread pool, work stealing, `Task.Run` in ASP.NET (why not)
- `cancellation` (S) — `CancellationToken` propagation, linked tokens, cooperative cancellation [kata]
- `concurrency-primitives` (S) — `lock`/`Monitor`, `SemaphoreSlim`, `Interlocked`, `ReaderWriterLockSlim`, `Channel<T>`
- `parallel-vs-async` (M) — CPU-bound vs IO-bound, `Parallel.ForEachAsync`, `IAsyncEnumerable`
- `async-pitfalls` (S) — `async void`, sync-over-async, `Task.WhenAll` exception behaviour, fire-and-forget

### 1.3 `csharp-lang` · Language Features
- `records-structs` (M) — records, record structs, `with`, equality, `readonly struct`, `ref struct`
- `pattern-matching` (M) — type/property/positional/list patterns, switch expressions
- `generics-variance` (S) — constraints, covariance/contravariance, `in`/`out`, static abstract members
- `delegates-events-lambdas` (M) — closures capture semantics, `Func`/`Action`, event vs delegate
- `linq-internals` (S) — deferred execution, `IEnumerable` vs `IQueryable`, expression trees, multiple enumeration
- `nullable-reference-types` (M) — annotations, `!`, `MaybeNull`, nullability in EF/DTOs
- `exceptions` (M) — cost, filters, `when`, custom exceptions, `ExceptionDispatchInfo`, result pattern vs exceptions
- `source-generators-reflection` (S) — reflection cost, `[GeneratedRegex]`, `System.Text.Json` source gen, AOT implications
- `modern-features` (M) — primary constructors, collection expressions, required members, `params` spans, extension members

### 1.4 `dotnet-platform` · Platform & Tooling
- `di-container` (S) — lifetimes, captive dependency, keyed services, `IServiceScopeFactory`, disposal order
- `configuration-options` (M) — providers, `IOptions`/`IOptionsSnapshot`/`IOptionsMonitor`, validation
- `logging-observability` (S) — `ILogger` structured logging, scopes, OpenTelemetry, `ActivitySource`, metrics
- `hosting-lifecycle` (M) — generic host, `IHostedService`, `BackgroundService`, graceful shutdown
- `performance-diagnostics` (S) — BenchmarkDotNet, `dotnet-counters`, `dotnet-trace`, allocation profiling, common hot paths
- `versioning-runtime` (M) — LTS/STS, TFMs, `global.json`, trimming, ReadyToRun, Native AOT trade-offs

---

## Domain 2 — `aspnet-data` · ASP.NET Core, EF Core & PostgreSQL

### 2.1 `pipeline` · Request Pipeline
- `middleware-order` (M) — pipeline, `Use`/`Run`/`Map`, ordering of auth/cors/exception handling
- `minimal-vs-controllers-fastendpoints` (M) — trade-offs, endpoint filters, REPR pattern
- `model-binding-validation` (M) — binding sources, `ProblemDetails`, FluentValidation placement
- `exception-handling` (S) — `IExceptionHandler`, `ProblemDetails`, mapping domain errors [kata]
- `custom-middleware` (M) — correlation id, request logging, idempotency key middleware [kata]
- `filters-vs-middleware` (M) — when each applies, endpoint filters in minimal APIs

### 2.2 `security` · Auth & Security
- `authn-vs-authz` (M) — schemes, handlers, policies, requirements, resource-based auth
- `jwt-oidc` (S) — JWT validation, refresh tokens, OIDC flows, `Microsoft.Identity.Web`, key rotation
- `cors-csrf` (M) — CORS preflight, antiforgery in SPA scenarios, cookie vs bearer
- `owasp-dotnet` (S) — injection, mass assignment, secrets management, rate limiting, security headers
- `data-protection` (M) — Data Protection API, key ring in containers

### 2.3 `efcore` · EF Core
- `change-tracking` (S) — tracked vs `AsNoTracking`, `Attach`/`Update`, identity resolution, `DetectChanges`
- `n-plus-one-loading` (S) — `Include`, split queries, projections, filtered includes, `AsSplitQuery`
- `query-translation` (S) — client vs server eval, `IQueryable` composition, raw SQL, `ExecuteUpdate/Delete`
- `migrations-strategy` (S) — idempotent scripts, migrations in CI/CD, bundles, zero-downtime schema changes
- `concurrency-transactions` (S) — optimistic concurrency (`xmin`), explicit transactions, `SaveChanges` semantics
- `dbcontext-lifetime` (M) — scoped context, pooling, `IDbContextFactory`, thread safety
- `mapping-advanced` (M) — owned types, value converters, JSON columns, TPH/TPT/TPC, global query filters
- `bulk-and-performance` (S) — compiled queries, batching, bulk libraries, when to bypass EF

### 2.4 `postgres` · PostgreSQL & SQL
- `indexes` (S) — B-tree, composite order, covering (`INCLUDE`), partial, GIN for JSONB, when indexes hurt
- `explain-analyze` (S) — reading plans, seq vs index scan, estimated vs actual rows [kata]
- `isolation-levels` (S) — MVCC, read committed vs repeatable read vs serializable, anomalies, `SELECT FOR UPDATE`
- `locks-deadlocks` (S) — row/table locks, deadlock detection, advisory locks, lock ordering
- `joins-window-cte` (M) — join types, window functions, CTEs, recursive CTE [kata]
- `jsonb` (M) — operators, indexing, when JSONB vs columns
- `pagination-search` (M) — offset vs keyset pagination, full-text search, `pg_trgm`
- `connection-pooling` (M) — Npgsql pool, PgBouncer, pool exhaustion symptoms

### 2.5 `api-design` · API Design & Integration
- `rest-conventions` (M) — resource naming, status codes, idempotency, PATCH semantics, versioning
- `openapi-contracts` (M) — OpenAPI generation, contract-first, client generation, breaking changes
- `httpclient-resilience` (S) — `IHttpClientFactory`, Polly/`Microsoft.Extensions.Resilience`, retries, timeouts, circuit breaker
- `background-jobs` (S) — hosted services vs Hangfire/Quartz, outbox for reliability, idempotent consumers
- `caching` (S) — `IMemoryCache`, `HybridCache`, Redis, cache stampede, invalidation strategies
- `realtime` (M) — SignalR, SSE, WebSockets trade-offs (SSE used by this platform for LLM streaming)
- `testing-aspnet` (S) — `WebApplicationFactory`, Testcontainers, test doubles vs real DB, contract tests

---

## Domain 3 — `angular` · Angular

### 3.1 `core` · Core Mechanics
- `change-detection` (S) — zone-based vs zoneless, `OnPush`, `markForCheck`, when CD runs, `ExpressionChanged` error
- `signals` (S) — `signal`, `computed`, `effect`, `linkedSignal`, `untracked`, signal inputs/outputs/model
- `di-injectors` (S) — hierarchical injectors, `providedIn`, element vs environment injector, `inject()`, injection tokens
- `lifecycle` (M) — hooks order, `afterRender`/`afterNextRender`, `DestroyRef`, `takeUntilDestroyed`
- `standalone-and-modules` (M) — standalone migration, `importProvidersFrom`, lazy-loading providers
- `control-flow-defer` (M) — `@if/@for/@switch`, `track`, `@defer` triggers and prefetch
- `templates-advanced` (M) — `ng-template`, `ng-container`, content projection, structural directives, host directives

### 3.2 `rxjs-state` · RxJS & State
- `rxjs-operators` (S) — `switchMap` vs `mergeMap` vs `concatMap` vs `exhaustMap`, `shareReplay`, `combineLatest`
- `subscriptions-leaks` (M) — unsubscribe strategies, `async` pipe, `takeUntilDestroyed`
- `signals-vs-rxjs` (S) — `toSignal`/`toObservable`, when to use each, interop pitfalls
- `state-management` (S) — service-with-signals pattern, when NgRx/SignalStore is justified, immutability
- `resource-api` (S) — `resource`, `httpResource`, loading/error states, request de-dup

### 3.3 `routing-forms-http` · Routing, Forms, HTTP
- `routing` (M) — lazy routes, guards (functional), resolvers, route params as inputs, preloading
- `reactive-forms` (M) — typed forms, custom validators, `ControlValueAccessor`, dynamic forms, signal forms
- `http-interceptors` (M) — functional interceptors, auth token, retry, error mapping [kata]
- `error-handling` (M) — `ErrorHandler`, global error UX, HTTP error mapping

### 3.4 `performance-arch` · Performance & Architecture
- `performance` (S) — bundle analysis, lazy loading, `@defer`, `trackBy`/`track`, virtual scroll, image directive
- `ssr-hydration` (S) — SSR, hydration, incremental hydration, `TransferState`, when SSR is worth it
- `project-structure` (S) — feature folders, smart/dumb components, facade pattern, Nx monorepo trade-offs
- `component-api-design` (S) — inputs/outputs contract, `model()`, host bindings, accessibility basics
- `testing-angular` (S) — TestBed, component harnesses, `HttpTestingController`, Jest/Vitest vs Karma, Playwright e2e
- `angular-vs-react` (M) — mental-model comparison to prepare for the React domain later

---

## Domain 4 — `architecture` · Architecture, Patterns & System Design

### 4.1 `principles` · Principles & Patterns
- `solid-in-practice` (M) — each principle with a violation example and a refactor
- `gof-patterns-that-matter` (M) — strategy, decorator, factory, builder, observer, mediator, specification; anti-patterns
- `clean-hexagonal-onion` (S) — dependency rule, ports/adapters, where DTOs live, over-engineering signals
- `ddd-lite` (S) — entities, value objects, aggregates, domain events, bounded contexts, anemic model
- `cqrs-and-mediator` (S) — when CQRS helps, when it is ceremony, read models
- `vertical-slice` (S) — feature folders vs layers, REPR, trade-offs against Clean Architecture
- `error-handling-strategy` (S) — result types vs exceptions, domain errors to HTTP mapping

### 4.2 `distributed` · Distributed Systems
- `idempotency-outbox` (S) — transactional outbox, inbox, idempotency keys, at-least-once delivery [kata]
- `messaging` (S) — queues vs pub/sub, RabbitMQ/Kafka/SQS trade-offs, ordering, DLQ, poison messages
- `consistency` (S) — CAP/PACELC, eventual consistency, sagas (orchestration vs choreography), compensations
- `caching-strategies` (S) — cache-aside, write-through, TTL vs invalidation, distributed cache pitfalls
- `resilience` (S) — timeouts, retries with jitter, circuit breaker, bulkhead, backpressure
- `observability` (S) — logs/metrics/traces, correlation, OpenTelemetry, SLO/SLI, alerting hygiene
- `api-gateway-bff` (M) — gateway, BFF, rate limiting, auth at the edge

### 4.3 `system-design` · System Design Interviews
- `design-process` (S) — requirements, estimation, high-level → deep dive, trade-off narration
- `scaling-basics` (S) — vertical vs horizontal, statelessness, load balancing, sticky sessions, DB read replicas
- `data-modeling-choices` (S) — SQL vs NoSQL, sharding, partitioning, hot keys
- `design-url-shortener` (S) — classic warm-up
- `design-notification-service` (S) — fan-out, retries, preferences, rate limits
- `design-carrier-integration-platform` (L) — your MailHub story: multi-carrier adapters, rate limiting per carrier, webhooks, retries, test mode
- `design-file-upload-processing` (S) — presigned URLs, async processing, status polling vs push

### 4.4 `lead` · Tech Lead Practice
- `code-review-culture` (L) — what to review, PR size, automation vs human review
- `tech-debt-management` (L) — classification, prioritisation, negotiating with product
- `adr-and-decision-making` (L) — ADRs, reversible vs irreversible decisions
- `estimation-and-planning` (L) — sizing, risk, spikes, saying no
- `mentoring-and-delegation` (L) — growing mids, pairing, feedback
- `story-bank` (L) — STAR stories: salary negotiation outcome, becoming lead, outbox introduction, AI workflow rollout, secrets migration, hard debugging (notes only, no quiz)

---

## Domain 5 — `ai-engineering` · Practical AI Engineering

### 5.1 `llm-foundations` · Foundations for Engineers
- `how-llms-work-enough` (M) — tokens, context window, temperature, sampling, why hallucinations happen
- `model-selection-cost` (S) — capability vs latency vs cost, routing, caching prompts, batch APIs
- `prompt-design` (S) — system vs user, few-shot, structured output/JSON schema, prompt versioning
- `context-engineering` (S) — what goes in context, memory files, progressive disclosure, context rot
- `evals` (S) — golden sets, LLM-as-judge, regression evals, why "it looks fine" is not a test

### 5.2 `patterns` · Application Patterns
- `rag` (S) — chunking, embeddings, hybrid search, reranking, citations, when RAG is the wrong tool
- `tool-use-agents` (S) — tool calling loop, agent vs workflow, planning, stopping criteria, cost control [kata]
- `structured-outputs-in-dotnet` (S) — typed contracts, validation, retries on invalid output, streaming (this platform's grading feature)
- `mcp` (S) — what MCP solves, servers vs clients, when to build one, security of tool exposure
- `guardrails-security` (S) — prompt injection, data exfiltration via tools, PII, least-privilege tools, human-in-the-loop
- `observability-for-llm` (M) — tracing calls, token accounting, latency, quality monitoring

### 5.3 `ai-sdlc` · AI in the Development Process
- `ai-assisted-workflow` (L) — your real setup: CLAUDE.md, memory bank, hooks, subagents, spec-driven development; where it saves time, where it does not
- `spec-driven-development` (S) — specs as source of truth, constitution, plan/tasks, review loops
- `ai-code-review` (S) — PR review pipelines, what AI catches, what it misses, false-positive management
- `testing-with-ai` (M) — generating tests, mutation of tests, verifying AI output
- `team-adoption` (L) — rollout, guardrails, measuring impact, common failure modes, skepticism handling
- `risks-and-limits` (S) — over-reliance, skill atrophy, licensing/IP, data leakage, vendor lock-in

---

## Later domains (not in MVP)
- `react` — hooks, rendering model, server components, state libs, Next.js
- `devops-cloud` — Docker, Kubernetes basics, CI/CD, AWS/Azure services, IaC
- `frontend-fundamentals` — HTTP, browser rendering, web security, TypeScript advanced types
