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

Not yet scaffolded — see [CLAUDE.md](CLAUDE.md) and
[../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
(Phase 0, then Phase 1) for what happens next.
