using Lakbay.Contracts;
using MongoDB.Driver;

namespace Lakbay.AvailabilityApi.Shared;

/// <summary>
/// Typed access to this service's MongoDB collections. Shared between the
/// query API (reads) and Lakbay.AvailabilityApi.Sync (writes, ADR-0009) so
/// both processes agree on collection names and document types from one
/// place. Registered as a singleton — <see cref="IMongoDatabase"/> is
/// already thread-safe and meant to be reused across the app's lifetime,
/// not created per request.
/// </summary>
public sealed class CatalogContext(IMongoDatabase database)
{
    public IMongoCollection<ProductLine> ProductLines { get; } = database.GetCollection<ProductLine>("productLines");
    public IMongoCollection<Country> Countries { get; } = database.GetCollection<Country>("countries");
    public IMongoCollection<Region> Regions { get; } = database.GetCollection<Region>("regions");
    public IMongoCollection<Destination> Destinations { get; } = database.GetCollection<Destination>("destinations");
    public IMongoCollection<Accommodation> Accommodations { get; } = database.GetCollection<Accommodation>("accommodations");
    public IMongoCollection<RoomType> RoomTypes { get; } = database.GetCollection<RoomType>("roomTypes");
    public IMongoCollection<Activity> Activities { get; } = database.GetCollection<Activity>("activities");
    public IMongoCollection<Product> Products { get; } = database.GetCollection<Product>("products");

    /// <summary>
    /// Product and Destination's natural key is already Mongo's own _id
    /// (MongoClassMaps maps Id -> _id), uniquely enforced for free.
    /// ProductLine has no Id property (ADR predates this file — see
    /// MongoClassMaps' comment); Code is its natural key instead, so it
    /// needs an explicit unique index. Without it, Lakbay.AvailabilityApi.Sync's
    /// upsert-if-newer pattern (ADR-0010) would insert a duplicate
    /// ProductLine document every time a stale/equal-timestamp event lost
    /// the race, instead of safely no-op'ing. `CreateOneAsync` with the
    /// same key is idempotent — safe to call on every startup.
    /// </summary>
    public Task EnsureIndexesAsync(CancellationToken ct = default) =>
        ProductLines.Indexes.CreateOneAsync(
            new CreateIndexModel<ProductLine>(
                Builders<ProductLine>.IndexKeys.Ascending(p => p.Code),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: ct);
}
