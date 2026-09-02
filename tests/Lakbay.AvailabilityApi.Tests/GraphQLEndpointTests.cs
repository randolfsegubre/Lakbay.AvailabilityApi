using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lakbay.AvailabilityApi.Tests;

/// <summary>
/// Phase 0's only real test: the GraphQL server boots and answers a
/// query. Real coverage (Product/ProductLine/Destination resolvers
/// against MongoDB, the schema-diff check against Lakbay.Contracts)
/// arrives in Phase 1 — see Lakbay.Docs/docs/02_BUILD_PLAN.md.
/// </summary>
public class GraphQLEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Status_query_returns_ok()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/graphql", new { query = "{ status }" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("\"status\":\"ok\"", body);
    }
}
