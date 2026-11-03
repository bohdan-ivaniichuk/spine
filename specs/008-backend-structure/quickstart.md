# Quickstart: Backend structure refactor

1. **SC-001** `dotnet test` — every test passes; `git diff dev -- tests` shows no changed assertion
   (only `using`, construction and URL-building lines).
2. **SC-002 / FR-007** the snapshot tests pass without `SPINE_SNAPSHOTS=record`; the case files under
   `tests/Api.Tests/Snapshots/cases/` are unchanged since they were first recorded.
3. **SC-003 / FR-008 / FR-009** `dotnet test tests/Spine.ArchitectureTests` passes; adding a second top-level type to
   any `src` file makes it fail naming the file.
4. **SC-004 / FR-004** `wc -l src/Api/Program.cs` < 50. `docker compose up -d --build`: api healthy on a fresh
   volume copy; `docker compose stop postgres && docker compose restart api` → the api logs "Database start-up failed;
   refusing to serve" and exits with code 1.
5. **US1** open `src/Api/Endpoints/Knowledge/GetConcept/` — three files; `ApiRoutes.Knowledge.Concept` is its route.
6. **Smoke** the web app at <http://localhost> works as before (review session, atlas, weak spots, activity card).
