using Lakbay.Contracts;
using HotChocolate;
using MongoDB.Driver;

namespace Lakbay.AvailabilityApi.Api;

/// <summary>
/// Real read-path resolvers, matching schema/lakbay.graphql's Query type
/// field-for-field. Pure reads against MongoDB — this project has no
/// Service Bus code and never will (ADR-0009); writes only ever happen
/// through Lakbay.AvailabilityApi.Sync.
/// </summary>
public class Query
{
    // Kept outside the formal Lakbay.Contracts schema on purpose — a
    // lightweight liveness field Lakbay.Web's Phase 0 smoke test
    // (useGetStatusQuery) already depends on. Remove once schema-diff CI
    // (see Lakbay.Docs/docs/04_TASKS.md) is wired up and Lakbay.Web has
    // moved onto a real query to check against instead.
    public string Status => "ok";

    public async Task<List<ProductLine>> GetProductLines(
        [Service] CatalogContext catalog,
        CancellationToken ct)
        => await catalog.ProductLines.Find(FilterDefinition<ProductLine>.Empty).ToListAsync(ct);

    public async Task<List<Destination>> GetDestinations(
        ProductLineCode? productLine,
        [Service] CatalogContext catalog,
        CancellationToken ct)
    {
        var filter = productLine is { } code
            ? Builders<Destination>.Filter.Eq(d => d.ProductLine, code)
            : FilterDefinition<Destination>.Empty;

        return await catalog.Destinations.Find(filter).ToListAsync(ct);
    }

    public async Task<List<Product>> GetProducts(
        ProductFilter? filter,
        [Service] CatalogContext catalog,
        CancellationToken ct)
        => await catalog.Products.Find(BuildFilter(filter)).ToListAsync(ct);

    public async Task<Product?> GetProduct(
        string slug,
        [Service] CatalogContext catalog,
        CancellationToken ct)
        => await catalog.Products.Find(p => p.Slug == slug).FirstOrDefaultAsync(ct);

    /// <summary>
    /// Translates ProductFilter (Lakbay.Contracts' explicit input type —
    /// deliberately not HotChocolate's auto-generated [UseFiltering],
    /// which would change this field's argument shape and break parity
    /// with the shared schema) into a MongoDB filter. Price/date bounds
    /// combine into a single PriceBands ElemMatch so they all have to be
    /// satisfied by the *same* band, not independently by any band.
    /// </summary>
    private static FilterDefinition<Product> BuildFilter(ProductFilter? filter)
    {
        var f = Builders<Product>.Filter;

        if (filter is null)
        {
            return f.Empty;
        }

        var clauses = new List<FilterDefinition<Product>>();

        if (filter.ProductLine is { } productLine)
        {
            clauses.Add(f.Eq(p => p.ProductLine, productLine));
        }

        if (filter.DestinationId is { } destinationId)
        {
            clauses.Add(f.Eq(p => p.Destination.Id, destinationId));
        }

        var bandClauses = new List<FilterDefinition<PriceBand>>();
        var bf = Builders<PriceBand>.Filter;

        if (filter.MinPricePhp is { } minPrice)
        {
            bandClauses.Add(bf.Gte(b => b.PricePhp, minPrice));
        }

        if (filter.MaxPricePhp is { } maxPrice)
        {
            bandClauses.Add(bf.Lte(b => b.PricePhp, maxPrice));
        }

        if (filter.StartDate is { } startDate)
        {
            bandClauses.Add(bf.Gte(b => b.EndDate, startDate));
        }

        if (filter.EndDate is { } endDate)
        {
            bandClauses.Add(bf.Lte(b => b.StartDate, endDate));
        }

        if (bandClauses.Count > 0)
        {
            var combinedBandFilter = bandClauses.Count == 1 ? bandClauses[0] : bf.And(bandClauses);
            clauses.Add(f.ElemMatch(p => p.PriceBands, combinedBandFilter));
        }

        if (filter.ExcludeSoldOut == true)
        {
            clauses.Add(f.Gt(p => p.AvailableCount, 0));
        }

        return clauses.Count == 0 ? f.Empty : f.And(clauses);
    }
}
