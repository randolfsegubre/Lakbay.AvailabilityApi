using Lakbay.Contracts;
using MongoDB.Driver;

namespace Lakbay.AvailabilityApi.Api;

/// <summary>
/// Typed access to this service's MongoDB collections. Registered as a
/// singleton — <see cref="IMongoDatabase"/> is already thread-safe and
/// meant to be reused across the app's lifetime, not created per request.
/// </summary>
public sealed class CatalogContext(IMongoDatabase database)
{
    public IMongoCollection<ProductLine> ProductLines { get; } = database.GetCollection<ProductLine>("productLines");
    public IMongoCollection<Destination> Destinations { get; } = database.GetCollection<Destination>("destinations");
    public IMongoCollection<Product> Products { get; } = database.GetCollection<Product>("products");
}
