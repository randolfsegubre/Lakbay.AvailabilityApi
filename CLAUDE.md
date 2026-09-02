# Lakbay.AvailabilityApi — Start Here

This file is intentionally short. It exists so any Claude Code session (or
other AI coding assistant) rooted here auto-loads it and is pointed at the
real documentation before touching anything.

**Read, in this order, before writing any code:**

1. [../Lakbay.Docs/docs/01_CLAUDE.md](../Lakbay.Docs/docs/01_CLAUDE.md) —
   the platform AI operating manual.
2. [ADR-0007](../Lakbay.Docs/docs/adr/ADR-0007-searchapi-is-real-not-mock.md)
   — **read this before assuming anything about this repo's scope.** It
   used to be planned as a disposable mock (`Lakbay.MockApi`); it isn't
   one. This is a real, permanently deployed search/query service modeled
   on Hotelplan's `api-sphinx` (Manticore-backed), not a throwaway.
3. [ADR-0009](../Lakbay.Docs/docs/adr/ADR-0009-availabilityapi-rename-and-split.md)
   — this repo is **two deployables**, not one: a GraphQL query API and a
   separate Azure Function (`Lakbay.AvailabilityApi.Sync`). Know which one
   you're working in before writing code — they have different jobs and
   different rules (see below).
4. [ADR-0010](../Lakbay.Docs/docs/adr/ADR-0010-last-write-wins-sync.md) —
   the sync function's ordering guard. Required reading before touching
   anything that writes to MongoDB in this repo.
5. [../Lakbay.Docs/docs/06_SYSTEM_ARCHITECTURE.md](../Lakbay.Docs/docs/06_SYSTEM_ARCHITECTURE.md)
   — this repo's section covers its internal shape and exactly how it
   fits the whole platform.
6. [../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
   — **Phase 0** (scaffolding, both projects), **Phase 1** (query API +
   resolvers + seed data), **Phase 4** (the sync function's real Service
   Bus subscriptions go live), and **Phase 5** (this repo goes live
   alongside `Lakbay.Cms`/`Lakbay.Booking`, it does not get retired).
7. [../Lakbay.Docs/docs/04_TASKS.md](../Lakbay.Docs/docs/04_TASKS.md) —
   current status across the whole platform.

## What this repo is

**Two projects, one repo, sharing MongoDB and `Lakbay.Contracts` types
(ADR-0009):**

- **The query API** — ASP.NET Core + HotChocolate + MongoDB.Driver. Pure
  read path: answers GraphQL queries against the denormalized catalog
  (holidays, product lines, destinations), with real faceted filtering —
  destination, theme, price, date — the way Hotelplan's `api-sphinx`/
  Manticore served that same need. **Contains no Service Bus code at
  all** — that's deliberate, so a burst of availability events never
  competes with shoppers' queries for resources.
- **`Lakbay.AvailabilityApi.Sync`** — an Azure Function
  (`[ServiceBusTrigger]`), the actual event consumer. Subscribes to
  `Lakbay.Cms` publish-sync events and `Lakbay.Booking`'s
  `AvailabilityChanged` events; for each, applies the update to MongoDB
  only if it's newer than what's stored (last-write-wins, ADR-0010), via
  a single atomic `findOneAndUpdate` — never a separate read-then-write.
  Fires the Azure SignalR push notification after a successful write.

`Lakbay.Cms` (Umbraco) stays the authoring source of truth. This repo is
kept in sync **from** `Lakbay.Cms` and `Lakbay.Booking`, not the other way
around — content editors never touch this repo or its data directly.

`Lakbay.Web` queries the query API for catalog browsing, search, and
filtering in production. It is **not** repointed away from once
`Lakbay.Cms` exists — both `Lakbay.Cms` and this repo stay in the
picture, each doing a different job (see `06_SYSTEM_ARCHITECTURE.md`).

**Non-negotiable:** CI runs a schema-diff check against
`Lakbay.Contracts`' published SDL on every change to the query API — this
keeps the read-model's schema from silently drifting out of sync with
`Lakbay.Cms`'s write-model schema.

**Every document this repo stores carries a `sourceUpdatedUtc` field**
(ADR-0010), copied from the source event, not generated at write time.
The sync function's write path is always "is the incoming value newer
than what's stored" — never a blind overwrite.

## Local setup

Not yet proven — Phase 0 is not complete. Once both projects boot locally
against MongoDB (Docker Compose), the exact commands go in
`Docs/DEVELOPER_HANDBOOK.md` (create that file the moment setup actually
works, not from memory afterward).

## End of session

Update `../Lakbay.Docs/docs/04_TASKS.md` and append an entry to
`../Lakbay.Docs/docs/05_DEVLOG.md` for anything that changed phase status
or made a new structural decision.
