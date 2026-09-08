# Lakbay.AvailabilityApi — Developer Handbook

Written the moment local setup actually worked (2026-09-06; Phase 1
resolvers added 2026-09-08). The test for this document: could you follow
it on a plane, no internet, no AI agent?

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

## Local setup — proven working, Phase 0 (2026-09-06) and Phase 1 (2026-09-08)

Requires: .NET SDK 10.x, Docker Desktop (for MongoDB — see below). **Still
not required**: Azure Functions Core Tools — only needed to actually run
`Lakbay.AvailabilityApi.Sync` locally, and only from Phase 4 on.

```bash
# 1. Start MongoDB (no auth — local-only, nothing secret to protect,
#    unlike Lakbay.Cms's SQL Server which needs a real password):
docker compose up -d
docker inspect --format='{{.State.Health.Status}}' lakbay_mongo   # wait for "healthy"

# 2. Build and test — the test suite spins up its OWN ephemeral MongoDB
#    via Testcontainers, so it never touches the compose-managed one above:
dotnet build     # 0 Warning(s), 0 Error(s) across all 3 projects
dotnet test tests/Lakbay.AvailabilityApi.Tests/Lakbay.AvailabilityApi.Tests.csproj
# 7 passed — productLines/destinations/products/product resolvers, all
# against a real seeded database, not mocks

# 3. Run the query API against the compose-managed MongoDB from step 1:
dotnet run --project src/Lakbay.AvailabilityApi.Api
# On first run against an empty database, CatalogSeeder inserts four
# ProductLine records and one real Destination/Product per line (Coron,
# Baguio, San Fernando Pampanga, Vigan) — a no-op on every run after.
```

Verified live, 2026-09-08 (`curl` against a running instance):

```bash
curl -s http://localhost:5170/graphql -H "Content-Type: application/json" \
  -d '{"query":"{ productLines { code name } }"}'
# → all four seeded lines, e.g. {"code":"ALON","name":"Alon"}, ...

curl -s http://localhost:5170/graphql -H "Content-Type: application/json" \
  -d '{"query":"{ products(filter: { productLine: AMIHAN, minPricePhp: 5000 }) { name } }"}'
# → only "Baguio Cool-Weather Weekend, 2 Days 1 Night" (PHP 6,800) —
#   correctly excludes anything below the price floor
```

**A second real gotcha, found only once a real client used it (2026-09-08):**
`products(filter: $filter)` worked fine in every manual `curl` test in
this handbook — because they all pass `filter` as an **inline literal**
(`products(filter: { productLine: AMIHAN, ... })`), which never triggers
GraphQL's variable-type-matching validation. The moment `Lakbay.Web` sent
it as a proper `$filter: ProductFilter` variable (the way a real client
should), it failed: `"The variable 'filter' is not compatible with the
type of the current location"`. HotChocolate's default naming convention
had named the input type `ProductFilterInput` (appending "Input" to any
inferred input object), not `ProductFilter` as `Lakbay.Contracts`' schema
declares. Fixed with an explicit type descriptor — `ProductFilterInputType
: InputObjectType<ProductFilter>` overriding `Name("ProductFilter")`,
registered via `.AddType<ProductFilterInputType>()` in `Program.cs` — the
same Adapter shape as `MongoClassMaps`, adjusting how this repo exposes a
`Lakbay.Contracts` type without putting a HotChocolate attribute on the
shared type itself. A regression test
(`Products_filter_works_as_a_real_graphql_variable_not_just_an_inline_literal`)
now exercises the real variable path specifically, since none of the
others did. **The lesson that generalizes:** a query that works via inline
literals is not proof a client using proper variables will also work —
verify with the actual variable syntax before trusting a curl smoke test.

**A real gotcha hit and fixed here:** `ProductLine` has no `Id` property
(its stable key is the `Code` enum), so it was left to Mongo's
auto-assigned `_id`. That broke `productLines` with an opaque "Unexpected
Execution Error" — `BsonClassMap` throws on deserialization for any
document field with no matching C# member unless `IgnoreExtraElements` is
set, and an unmapped `_id` is exactly that. Fixed in `MongoClassMaps.cs`
with `cm.SetIgnoreExtraElements(true)` on `ProductLine`'s map specifically
— `Destination` and `Product` don't need it because their `Id` *is*
mapped to `_id`. Worth knowing before it looks like a mystery a second
time on some future type that also lacks a natural id.

**CORS:** `Lakbay.Web` calls this API cross-origin in local dev
(`localhost:3000` → this API's port). The allowed origin list comes from
`Cors:AllowedOrigins` in config — `appsettings.Development.json` sets it
to `["http://localhost:3000"]`. Run with `ASPNETCORE_ENVIRONMENT=Development`
(the default for plain `dotnet run`; not automatic with
`--no-launch-profile`) or that config won't load and the browser preflight
will 404.

`Lakbay.AvailabilityApi.Sync` has a real function since Phase 4 —
`CatalogSyncFunction` (`[ServiceBusTrigger("lakbay-catalog-sync", ...)]`),
consuming `Lakbay.Cms`'s publish-sync events. Run it locally with
`func start` (from `src/Lakbay.AvailabilityApi.Sync`) against the Azure
Service Bus emulator (`docker compose up -d` in this repo also brings up
`servicebus-emulator` + its `sqledge` companion, not just MongoDB) —
verified live 2026-09-09, listed as `CatalogSyncFunction: serviceBusTrigger`
on startup.

## What's genuinely still blocked on external tooling

Nothing, as of 2026-09-09 — Azure Functions Core Tools (`func` CLI) and the
Azure Service Bus emulator (Docker) are both installed/configured and
proven working locally.

## Adding a new query field — worked walkthrough

This is the real sequence used to add the four Phase 1 fields
(`productLines`, `destinations`, `products`, `product`) — follow the same
steps for the next one:

1. Add the field to `Query` in `Lakbay.Contracts/schema/lakbay.graphql`,
   with a description string if its meaning isn't obvious from the name.
2. Add/update the matching C# type in `Lakbay.Contracts/csharp/` by hand
   (no SDL-to-C# codegen yet — see that repo's own handbook) and
   `dotnet build` there to confirm it compiles.
3. Regenerate `Lakbay.Contracts`' TypeScript side (`npm run codegen` in
   `typescript/`) even if `Lakbay.Web` doesn't consume the new field yet —
   keeps both sides of the contract moving together, not one repo ahead.
4. Add the resolver method to `Query.cs` here — a plain method taking
   `[Service] CatalogContext catalog` (and any GraphQL arguments) and
   returning the field's type; HotChocolate wires it into the schema
   automatically by reflection, no manual type registration needed for a
   plain object return type.
5. If the field needs custom filtering (not a plain lookup), build the
   `FilterDefinition<T>` by hand from the input type's fields — see
   `Query.BuildFilter` — rather than reaching for HotChocolate's
   `[UseFiltering]`, which auto-generates its own argument shape and would
   break parity with `Lakbay.Contracts`' explicit schema.
6. If the new field needs seed data to be testable, add it to
   `CatalogSeeder.cs` — real Philippine data, not placeholders (see the
   existing four destinations for the bar).
7. Write a test in `CatalogQueryTests.cs` using the existing
   `[Collection("Mongo")]` + `AvailabilityApiFactory` pattern — each test
   gets its own isolated database name via `[CallerMemberName]`, so it
   never depends on another test's state.
