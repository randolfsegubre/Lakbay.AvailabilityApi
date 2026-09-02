# Lakbay.MockApi — Start Here

This file is intentionally short. It exists so any Claude Code session (or
other AI coding assistant) rooted here auto-loads it and is pointed at the
real documentation before touching anything.

**Read, in this order, before writing any code:**

1. [../Lakbay.Docs/docs/01_CLAUDE.md](../Lakbay.Docs/docs/01_CLAUDE.md) —
   the platform AI operating manual. Constitution for the whole Lakbay
   estate; if anything else conflicts with it, it wins unless the user
   explicitly overrides it in the current conversation.
2. [../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
   — **Phase 0** (scaffolding) and **Phase 1** (this repo's real work —
   resolvers + seeded data, before `Lakbay.Web` or `Lakbay.Cms` exist in
   any usable form) are this repo's phases.
3. [../Lakbay.Docs/docs/04_TASKS.md](../Lakbay.Docs/docs/04_TASKS.md) —
   current status across the whole platform.
4. [ADR-0004](../Lakbay.Docs/docs/adr/ADR-0004-mockapi-dotnet-not-node.md)
   — why this repo is ASP.NET Core + HotChocolate, not Node.js/Apollo
   (that was the original plan; it changed 2026-09-05).

## What this repo is

The Sphinx-API/Mantincore pattern, carried forward: a disposable
**ASP.NET Core + HotChocolate + MongoDB.Driver** GraphQL service that
mirrors the `Lakbay.Contracts` schema exactly. Its entire reason to exist
is so `Lakbay.Web` can be built, tested, and demoed before `Lakbay.Cms`/
`Lakbay.Booking` are ready. **Never deployed to production** — see
`02_BUILD_PLAN.md` Phase 5.

Same backend language as every other repo except `Lakbay.Web` (ADR-0004)
— use `HotChocolate.Data.MongoDb` for querying, not a hand-rolled resolver
layer, and reuse the same Repository/Dependency-Inversion shape documented
in `03_ARCHITECTURE_AND_PATTERNS_GUIDE.md` rather than inventing a
different pattern just because this service is "only a mock."

**Non-negotiable:** CI runs a schema-diff check against
`Lakbay.Contracts`' published SDL on every change. If this repo's schema
and the real backend's schema silently drift apart, the entire point of
building `Lakbay.Web` against this service first is defeated.

**Seed data must be real**, not lorem — at minimum one real destination
per product line (Alon, Amihan, Parul, Pamana), sourced from the market
research in the published Lakbay Blueprint artifact. This data will be
visible in `Lakbay.Web`'s early screenshots and demos.

## Local setup

Not yet proven — Phase 0 is not complete. Once the ASP.NET Core +
HotChocolate baseline boots locally against MongoDB (Docker Compose), the
exact commands go in `Docs/DEVELOPER_HANDBOOK.md` (create that file the
moment setup actually works, not from memory afterward).

## End of session

Update `../Lakbay.Docs/docs/04_TASKS.md` and append an entry to
`../Lakbay.Docs/docs/05_DEVLOG.md` for anything that changed phase status
or made a new structural decision.
