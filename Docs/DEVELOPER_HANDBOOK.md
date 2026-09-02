# Lakbay.AvailabilityApi — Developer Handbook

Written the moment local setup actually worked (2026-09-06). The test for
this document: could you follow it on a plane, no internet, no AI agent?

## Layout

Two deployables, one repo (ADR-0009):

```
Lakbay.AvailabilityApi.sln
src/Lakbay.AvailabilityApi.Api/    HotChocolate GraphQL query API (net10.0)
                                    — pure read path, NO Service Bus code
src/Lakbay.AvailabilityApi.Sync/   Azure Functions isolated worker (net10.0)
                                    — Service-Bus-triggered event consumer
                                    (no functions defined yet — the exact
                                    trigger topic/subscription is a Phase 1/4
                                    decision, not made yet)
tests/Lakbay.AvailabilityApi.Tests/ xUnit, tests the Api project via
                                    WebApplicationFactory<Program>
```

Both `Api` and `Sync` reference `../Lakbay.Contracts/csharp/Lakbay.Contracts.csproj`
directly (project reference, no package registry needed yet).

## Local setup — proven working, 2026-09-06

Requires: .NET SDK 10.x. **Not required for this much**: MongoDB, Docker,
Azure Functions Core Tools — those are needed once Phase 1 adds real
resolvers and Phase 4 adds the real Service Bus trigger, not for this
Phase 0 skeleton.

```bash
dotnet build     # 0 Warning(s), 0 Error(s) across all 3 projects
dotnet test tests/Lakbay.AvailabilityApi.Tests/Lakbay.AvailabilityApi.Tests.csproj
# 1 passed — GraphQL server boots, { status } query returns "ok"

dotnet run --project src/Lakbay.AvailabilityApi.Api
# POST http://localhost:<port>/graphql  { "query": "{ status }" }
# → {"data":{"status":"ok"}}
```

**CORS:** `Lakbay.Web` calls this API cross-origin in local dev
(`localhost:3000` → `localhost:5000`). The allowed origin list comes from
`Cors:AllowedOrigins` in config — `appsettings.Development.json` sets it
to `["http://localhost:3000"]`. Run with `ASPNETCORE_ENVIRONMENT=Development`
(not the default when using `dotnet run --no-launch-profile`) or that
config won't load and the browser preflight will 404.

**Verified end-to-end, 2026-09-06:** ran this API on `:5000` and
`Lakbay.Web`'s dev server on `:3000` simultaneously; the homepage's
`useGetStatusQuery()` call successfully round-tripped through RTK Query →
this API's `{ status }` field and rendered "Lakbay.AvailabilityApi says:
ok" in the browser.

`Lakbay.AvailabilityApi.Sync` builds but has no functions defined yet —
running it (`func start`, once Azure Functions Core Tools is installed —
not present on this machine as of 2026-09-06) would boot successfully but
do nothing, correctly, until Phase 4 adds the real trigger.

## What's genuinely still blocked on external tooling

- **MongoDB** (Phase 1 resolvers): via Docker Compose once Docker Desktop
  is installed, or a native MongoDB Community Server install.
- **Azure Functions Core Tools** (`func` CLI, for running/debugging
  `Lakbay.AvailabilityApi.Sync` locally): not installed on this machine as
  of 2026-09-06. `dotnet build` doesn't need it; actually running the
  Function locally does.
- **A real Service Bus emulator or namespace** (Phase 4): the Azure
  Service Bus emulator can run in Docker for fully offline local dev, once
  Docker is available.

## Adding a new query field — worked walkthrough (once Phase 1 starts)

Not applicable yet — `Query` is a one-field placeholder as of Phase 0.
This section gets filled in with a real, proven walkthrough (add the field
to `schema/lakbay.graphql` in `Lakbay.Contracts`, add the matching C#
type/property there, add the resolver here, add the seed data) the moment
Phase 1 actually does this once — not written speculatively now.
