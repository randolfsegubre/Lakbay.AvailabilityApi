# Lakbay.SearchApi — Start Here

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
3. [../Lakbay.Docs/docs/06_SYSTEM_ARCHITECTURE.md](../Lakbay.Docs/docs/06_SYSTEM_ARCHITECTURE.md)
   — this repo's section covers its internal shape and exactly how it
   fits the whole platform.
4. [../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
   — **Phase 0** (scaffolding), **Phase 1** (resolvers, seed data, and the
   real sync mechanism from `Lakbay.Cms` — this is now genuine scope, not
   a "just seed some fake data" step), and **Phase 5** (this repo goes
   live alongside `Lakbay.Cms`/`Lakbay.Booking`, it does not get retired).
5. [../Lakbay.Docs/docs/04_TASKS.md](../Lakbay.Docs/docs/04_TASKS.md) —
   current status across the whole platform.

## What this repo is

**ASP.NET Core + HotChocolate + MongoDB.Driver** (stack decided in
ADR-0004, role corrected in ADR-0007). A dedicated, read-optimized product
search service: it holds a denormalized copy of the catalog (holidays,
product lines, destinations), built and indexed for fast faceted
filtering — destination, theme, price, date — the way Hotelplan's
`api-sphinx`/Manticore served that same need, rather than querying
Umbraco's general-purpose content index directly for every storefront
search.

`Lakbay.Cms` (Umbraco) stays the authoring source of truth. This repo is
kept in sync **from** `Lakbay.Cms`, not the other way around — content
editors never touch this repo or its data directly. The exact sync trigger
(Umbraco event, Service Bus message, or scheduled job) is an open Phase 1
decision — see `02_BUILD_PLAN.md`.

`Lakbay.Web` queries this repo for catalog browsing, search, and
filtering in production. It is **not** repointed away from once
`Lakbay.Cms` exists — both stay in the picture, each doing a different
job (see `06_SYSTEM_ARCHITECTURE.md`).

**Non-negotiable:** CI runs a schema-diff check against
`Lakbay.Contracts`' published SDL on every change — this keeps the search
read-model's schema from silently drifting out of sync with
`Lakbay.Cms`'s write-model schema.

**Two Service Bus subscriptions, not one:** `Lakbay.Cms` publish-sync
(catalog/content changes) and, from Phase 4 onward, `Lakbay.Booking`'s
`AvailabilityChanged` events. Both update this repo's MongoDB read model;
both trigger the same Azure SignalR push-notification path afterward —
see [ADR-0008](../Lakbay.Docs/docs/adr/ADR-0008-realtime-availability-propagation.md).
This repo is the single place that knows the current read-model state and
is responsible for telling connected browsers when it changes.

## Local setup

Not yet proven — Phase 0 is not complete. Once the ASP.NET Core +
HotChocolate baseline boots locally against MongoDB (Docker Compose), the
exact commands go in `Docs/DEVELOPER_HANDBOOK.md` (create that file the
moment setup actually works, not from memory afterward).

## End of session

Update `../Lakbay.Docs/docs/04_TASKS.md` and append an entry to
`../Lakbay.Docs/docs/05_DEVLOG.md` for anything that changed phase status
or made a new structural decision.
