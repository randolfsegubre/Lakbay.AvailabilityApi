using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Lakbay.AvailabilityApi.Tests;

/// <summary>
/// A WebApplicationFactory pointed at a real, ephemeral Testcontainers
/// MongoDB instance instead of appsettings.Development.json's local-dev
/// connection string. Each caller passes its own database name so tests
/// never see another test's seeded/inserted data, even though they share
/// one running Mongo container (<see cref="MongoDbFixture"/>).
/// </summary>
public sealed class AvailabilityApiFactory(string mongoConnectionString, string databaseName)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = mongoConnectionString,
                ["Mongo:DatabaseName"] = databaseName,
            });
        });
    }
}
