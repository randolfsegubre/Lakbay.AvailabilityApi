using Testcontainers.MongoDb;

namespace Lakbay.AvailabilityApi.Tests;

/// <summary>
/// One real MongoDB instance (via Testcontainers), shared across every
/// test class in the "Mongo" collection — starting a fresh container per
/// test class would be needlessly slow. Real database, not a mock, per
/// the Lakbay Blueprint's TDD section.
/// </summary>
public sealed class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("Mongo")]
public sealed class MongoCollection : ICollectionFixture<MongoDbFixture>;
