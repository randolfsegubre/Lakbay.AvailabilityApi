using System.Net.Http.Json;

namespace Lakbay.AvailabilityApi.Tests;

/// <summary>
/// The liveness field kept outside the formal Lakbay.Contracts schema
/// (see Query.cs) — Lakbay.Web's Phase 0 smoke test still depends on it.
/// Uses the same isolated-Mongo pattern as CatalogQueryTests (not the
/// bare WebApplicationFactory default, which would silently depend on
/// whatever's on localhost:27017) even though this test never touches
/// the database — consistency here means nobody has to remember which
/// pattern applies to which test class.
/// </summary>
[Collection("Mongo")]
public class GraphQLEndpointTests(MongoDbFixture mongo)
{
    [Fact]
    public async Task Status_query_returns_ok()
    {
        var factory = new AvailabilityApiFactory(mongo.ConnectionString, "lakbay_test_status");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/graphql", new { query = "{ status }" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("\"status\":\"ok\"", body);
    }
}
