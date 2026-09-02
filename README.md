# Lakbay.MockApi

Disposable GraphQL + MongoDB backend for the Lakbay platform — the
Sphinx-API/Mantincore pattern carried forward. Lets `Lakbay.Web` be built
and tested before `Lakbay.Cms`/`Lakbay.Booking` exist. Dev/demo only,
never deployed to production.

**Stack:** ASP.NET Core + HotChocolate + MongoDB.Driver — same backend
language as the rest of the platform. See
[ADR-0004](../Lakbay.Docs/docs/adr/ADR-0004-mockapi-dotnet-not-node.md).

Not yet scaffolded — see [CLAUDE.md](CLAUDE.md) and
[../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
(Phase 0, then Phase 1) for what happens next.
