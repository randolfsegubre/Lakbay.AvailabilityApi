# Code Walkthrough — Lakbay.AvailabilityApi

**Two independent deployables in one repo** (ADR-0009) - know which one
you're reading before assuming anything:

```
src/Lakbay.AvailabilityApi.Api/     GraphQL query API - pure reads, no Service Bus code at all
src/Lakbay.AvailabilityApi.Sync/    Azure Function - the ONLY writer, Service-Bus-triggered
src/Lakbay.AvailabilityApi.Shared/  MongoDB access both of the above share (one copy, not two)
```

The split exists so a burst of sync writes (an editor bulk-publishing
content in `Lakbay.Cms`) can never compete with shoppers' queries for the
same process's CPU/threads - they're not just separate classes, they're
separate `dotnet run`/`func start` processes with separate host
lifecycles. See the platform-level `Lakbay.Docs/WALKTHROUGH.md` for how
this fits with `Lakbay.Cms`/`Lakbay.Web`.

## The query API - following a GraphQL request

1. **`Api/Program.cs`** wires `MongoClassMaps.Register()` (must run before
   any Mongo read/write anywhere in the process), a singleton
   `CatalogContext`, and HotChocolate (`AddGraphQLServer().AddQueryType<Query>()`).
2. **`Api/Query.cs`** - every resolver method here is a thin function:
   build a MongoDB filter from the GraphQL arguments, run it, return the
   result. `BuildFilter` is the one non-trivial piece - it combines
   price/date bounds into a single `PriceBands` `ElemMatch` so they all
   have to be satisfied by the *same* band, not independently by any band
   in the array.
3. **`Api/ProductFilterInputType.cs`** exists purely to fix a naming
   mismatch: HotChocolate's default convention would serve
   `Lakbay.Contracts.ProductFilter` as GraphQL type `ProductFilterInput`
   (auto-appending "Input"), not `ProductFilter` as the shared schema
   declares - this type descriptor overrides that. Read its own comment
   before touching `ProductFilter` naming anywhere.
4. **`Shared/CatalogContext.cs`** is the one place collection names
   (`"products"`, `"destinations"`, etc.) are defined, and the one place
   `ProductLine`'s unique index (on `Code`, since it has no natural `Id`)
   gets created.

## The Sync function - following a Service Bus message

1. **`Sync/Program.cs`** builds an isolated-worker Functions host
   (`FunctionsApplication.CreateBuilder` - not the older
   `ConfigureFunctionsWorkerDefaults` pattern), registers the same
   `CatalogContext`/`MongoClassMaps` the query API uses, and calls
   `EnsureIndexesAsync()` once at startup - this is *the* place that index
   actually gets created, since the read-only query API never should.
2. **`Sync/CatalogSyncFunction.cs`**'s `Run` method is the entry point,
   triggered by `[ServiceBusTrigger("lakbay-catalog-sync", ...)]`.
   Deserializes the message into a `CatalogSyncEvent`, switches on
   `EntityType`, and dispatches to one `Apply*Async` method per catalog
   type (`ApplyProductAsync`, `ApplyDestinationAsync`, etc.).
3. **Every `Apply*Async` method follows the identical shape**: build a
   MongoDB filter (`Eq(x => x.Id, incoming.Id)` AND
   `Lt(x => x.SourceUpdatedUtc, incoming.SourceUpdatedUtc)`), build an
   `Update` with one `.Set(...)` per field, then call the shared
   `UpsertIfNewerAsync` helper. This is ADR-0010's last-write-wins
   guarantee: if the stored document is already the same age or newer,
   the filter simply doesn't match, the upsert's insert attempt collides
   with the document's own unique key (`_id` for most types, an explicit
   index for `ProductLine.Code`), and the resulting `DuplicateKeyException`
   is caught and logged as an expected, silent discard - never a failure.
4. **`ApplyProductAsync` is the one exception worth knowing about**
   (ADR-0014): it never sets `AvailableCount`, even though the incoming
   event carries a value for it. `Lakbay.Booking` is `Product`'s other
   writer for that one field, so this function only ever `$sets` the
   catalog fields it actually owns - `SetOnInsert` seeds `AvailableCount`
   to `0` on first insert only, and leaves it untouched on every update.

## The "why" behind `MongoClassMaps.SetIgnoreExtraElements(true)`

Every registered type in `Shared/MongoClassMaps.cs` sets this. Two real
reasons converge here: `ProductLine` has no `Id` property (its stable key
is `Code`), so Mongo assigns its own auto `_id`, which has no matching C#
member - without `IgnoreExtraElements`, deserializing that document throws
the moment `BsonClassMap` sees a field it can't map. `Product` needs it for
a related but different reason (ADR-0014) - `Lakbay.Booking` may someday
write an internal-only field this repo's `Product` record doesn't know
about, and reads should never break because of it.
