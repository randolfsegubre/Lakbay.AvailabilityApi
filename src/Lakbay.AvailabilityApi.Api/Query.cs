namespace Lakbay.AvailabilityApi.Api;

/// <summary>
/// Phase 0 placeholder — just enough for a valid schema and a working
/// introspection query. Real fields (productLines, destinations,
/// products, product(slug:)) matching Lakbay.Contracts' schema arrive in
/// Phase 1, backed by MongoDB via HotChocolate.Data.MongoDb.
/// </summary>
public class Query
{
    public string Status => "ok";
}
