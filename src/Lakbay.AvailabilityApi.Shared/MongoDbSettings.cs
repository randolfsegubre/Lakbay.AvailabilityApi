namespace Lakbay.AvailabilityApi.Shared;

/// <summary>Bound from the "Mongo" configuration section.</summary>
public sealed class MongoDbSettings
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
}
