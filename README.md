# Lakbay.AvailabilityApi

A real, permanently deployed product-search service for the Lakbay
platform — modeled on Hotelplan's `api-sphinx` (Manticore-backed search
for price/availability/product filtering), using MongoDB in place of
Manticore. **Not a mock** — see
[ADR-0007](../Lakbay.Docs/docs/adr/ADR-0007-searchapi-is-real-not-mock.md)
for why this repo was renamed from `Lakbay.MockApi` and what changed.

**Stack:** ASP.NET Core + HotChocolate + MongoDB.Driver — same backend
language as the rest of the platform. See
[ADR-0004](../Lakbay.Docs/docs/adr/ADR-0004-mockapi-dotnet-not-node.md).

**Two deployables:** a pure query-serving GraphQL API, and a separate
Service-Bus-triggered Azure Function (`Lakbay.AvailabilityApi.Sync`) that
owns all event consumption — so query performance is never affected by
sync/write load. See
[ADR-0009](../Lakbay.Docs/docs/adr/ADR-0009-availabilityapi-rename-and-split.md)
and [ADR-0010](../Lakbay.Docs/docs/adr/ADR-0010-last-write-wins-sync.md)
(last-write-wins ordering for the sync side).

Both deployables are now real and running: the query API serves
`productLines`/`destinations`/`products`/`accommodations`/`activities`
resolvers against a live MongoDB synced from the real 14-product
`Lakbay.Cms` catalog, and `Lakbay.AvailabilityApi.Sync`'s
`[ServiceBusTrigger]` function is live, consuming real Cms publish events.
See [Docs/DEVELOPER_HANDBOOK.md](Docs/DEVELOPER_HANDBOOK.md) for proven
local setup, and [CLAUDE.md](CLAUDE.md) /
[../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
for full phase status.

## E2E testing

Verified live 2026-09-12 against the full pipeline (`Lakbay.Cms` → Service
Bus → `Sync` → MongoDB → this API): `Sync` started clean against the real
Service Bus emulator, and the GraphQL API returned all 14 real products
with correct names/slugs — confirming the whole chain is intact, not just
this repo in isolation. `availableCount: 0`/`isSoldOut: true` on every real
product is expected, not a bug: `Lakbay.Booking`'s own availability field
(ADR-0014) was only ever populated for its own small demo dataset, since
real online checkout was never built (ADR-0026 stubs `IPaymentGateway`).
Also exercised live by `Lakbay.AgentOps`'s offer-aggregation endpoint,
which queries three separate collections here (accommodations/products/
activities) in one call. Full trail:
[`../Lakbay.Docs/docs/05_DEVLOG.md`](../Lakbay.Docs/docs/05_DEVLOG.md)'s
2026-09-12 entry.
