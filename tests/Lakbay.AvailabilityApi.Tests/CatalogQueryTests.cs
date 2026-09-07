using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lakbay.AvailabilityApi.Tests;

/// <summary>
/// Phase 1's real coverage: the productLines/destinations/products/product
/// resolvers against a real MongoDB (Testcontainers), seeded via
/// CatalogSeeder — the same seeding path local dev uses, not test-only
/// fixtures, so a passing test means the resolvers work the way a
/// developer will actually see them.
/// </summary>
[Collection("Mongo")]
public class CatalogQueryTests(MongoDbFixture mongo)
{
    private async Task<HttpClient> CreateClientAsync([CallerMemberName] string testName = "")
    {
        var factory = new AvailabilityApiFactory(mongo.ConnectionString, DatabaseNameFor(testName));
        await factory.SeedCatalogAsync();
        return factory.CreateClient();
    }

    /// <summary>
    /// MongoDB caps database names at 63 characters — a real, previously-
    /// invisible bug found the moment test isolation actually started
    /// working (a prior Program.cs bug meant every test silently shared
    /// one real database, so this length was never actually exercised).
    /// Long test method names get truncated with an 8-char hash suffix
    /// for uniqueness rather than shortened arbitrarily, so two long
    /// names that happen to share a prefix still can't collide.
    /// </summary>
    private static string DatabaseNameFor(string testName)
    {
        const string prefix = "lakbay_test_";
        const int maxNameLength = 63;
        var name = (prefix + testName).ToLowerInvariant();

        if (name.Length <= maxNameLength)
        {
            return name;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(testName)))[..8];
        var truncatedTestName = testName[..(maxNameLength - prefix.Length - hash.Length - 1)];
        return $"{prefix}{truncatedTestName}_{hash}".ToLowerInvariant();
    }

    [Fact]
    public async Task ProductLines_returns_all_four_seeded_lines()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsJsonAsync("/graphql", new { query = "{ productLines { code } }" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var codes = body.GetProperty("data").GetProperty("productLines")
            .EnumerateArray()
            .Select(x => x.GetProperty("code").GetString())
            .ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("ALON", codes);
        Assert.Contains("AMIHAN", codes);
        Assert.Contains("PARUL", codes);
        Assert.Contains("PAMANA", codes);
    }

    [Fact]
    public async Task Product_by_slug_returns_the_real_seeded_product()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "{ product(slug: \"coron-island-hopping-3d2n\") { name destination { name } } }",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var product = body.GetProperty("data").GetProperty("product");

        Assert.Equal("Coron Island Hopping, 3 Days 2 Nights", product.GetProperty("name").GetString());
        Assert.Equal("Coron, Palawan", product.GetProperty("destination").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Product_by_unknown_slug_returns_null_not_an_error()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "{ product(slug: \"does-not-exist\") { name } }",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").GetProperty("product").ValueKind);
    }

    [Fact]
    public async Task Destinations_filter_by_product_line()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "{ destinations(productLine: PAMANA) { name } }",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.GetProperty("data").GetProperty("destinations")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToList();

        Assert.Single(names);
        Assert.Equal("Vigan", names[0]);
    }

    [Fact]
    public async Task Products_filter_combines_product_line_and_min_price_correctly()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "{ products(filter: { productLine: AMIHAN, minPricePhp: 5000 }) { name } }",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.GetProperty("data").GetProperty("products")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToList();

        Assert.Single(names);
        Assert.Equal("Baguio Cool-Weather Weekend, 2 Days 1 Night", names[0]);
    }

    [Fact]
    public async Task Products_filter_works_as_a_real_graphql_variable_not_just_an_inline_literal()
    {
        // Regression test for a real bug: HotChocolate's default naming
        // convention named the input type "ProductFilterInput", not
        // "ProductFilter" as Lakbay.Contracts' schema declares. Every
        // other test in this file passes `filter` as an inline literal
        // (`products(filter: { productLine: ... })`), which never
        // exercises GraphQL's variable-type-matching validation — only a
        // real client sending a `$filter: ProductFilter` variable
        // (exactly what Lakbay.Web does) hits it. Fixed via
        // ProductFilterInputType; this test is what would have caught it
        // before a real client did.
        var client = await CreateClientAsync();

        const string query = "query($filter: ProductFilter) { products(filter: $filter) { name } }";
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query,
            variables = new { filter = new { productLine = "ALON" } },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(body.TryGetProperty("errors", out _), body.ToString());
        var names = body.GetProperty("data").GetProperty("products")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("Coron Island Hopping, 3 Days 2 Nights", names);
    }

    [Fact]
    public async Task Products_filter_excludes_a_product_whose_price_band_is_below_the_floor()
    {
        var client = await CreateClientAsync();

        // Giant Lantern Festival Day Trip is PHP 2,200 — below a 5,000 floor.
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "{ products(filter: { productLine: PARUL, minPricePhp: 5000 }) { name } }",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var products = body.GetProperty("data").GetProperty("products").EnumerateArray().ToList();

        Assert.Empty(products);
    }
}
