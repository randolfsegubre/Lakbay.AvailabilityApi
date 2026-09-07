using System.Text.Json;
using Lakbay.AvailabilityApi.Shared;
using Lakbay.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Lakbay.AvailabilityApi.Sync;

/// <summary>
/// Consumes Lakbay.Cms's publish-sync events (ADR-0013) from the
/// <c>lakbay-catalog-sync</c> queue and applies them to the same MongoDB
/// the query API reads from — never through the query API itself
/// (ADR-0009). Lakbay.Booking's (Phase 4) AvailabilityChanged events will
/// arrive via a sibling function on a separate queue, not this one.
/// </summary>
public sealed class CatalogSyncFunction(CatalogContext catalog, ILogger<CatalogSyncFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    [Function(nameof(CatalogSyncFunction))]
    public Task Run(
        [ServiceBusTrigger("lakbay-catalog-sync", Connection = "ServiceBusConnection")] string messageBody,
        CancellationToken ct)
    {
        var syncEvent = JsonSerializer.Deserialize<CatalogSyncEvent>(messageBody, JsonOptions)
            ?? throw new InvalidOperationException("Empty or malformed CatalogSyncEvent payload — dead-lettering.");

        return syncEvent.EntityType switch
        {
            CatalogEntityType.ProductLine => ApplyProductLineAsync(
                syncEvent.ProductLine ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Country => ApplyCountryAsync(
                syncEvent.Country ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Region => ApplyRegionAsync(
                syncEvent.Region ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Destination => ApplyDestinationAsync(
                syncEvent.Destination ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Accommodation => ApplyAccommodationAsync(
                syncEvent.Accommodation ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.RoomType => ApplyRoomTypeAsync(
                syncEvent.RoomType ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Activity => ApplyActivityAsync(
                syncEvent.Activity ?? throw MissingPayload(syncEvent), ct),
            CatalogEntityType.Product => ApplyProductAsync(
                syncEvent.Product ?? throw MissingPayload(syncEvent), ct),
            _ => throw new InvalidOperationException($"Unknown CatalogEntityType: {syncEvent.EntityType}"),
        };
    }

    private static InvalidOperationException MissingPayload(CatalogSyncEvent e) =>
        new($"CatalogSyncEvent declared EntityType={e.EntityType} but its payload was null.");

    /// <summary>
    /// ProductLine: single writer (Lakbay.Cms only), so ADR-0010's
    /// original whole-document last-write-wins guard applies as designed.
    /// Upsert-if-newer via a filter that only matches an older-or-absent
    /// document; on a lost race against a concurrent/duplicate delivery
    /// (existing document is already equal-or-newer), Mongo's own unique
    /// index on Code (see CatalogContext.EnsureIndexesAsync) turns the
    /// resulting duplicate-insert attempt into a clean, expected
    /// DuplicateKey error — caught and logged, not rethrown, per ADR-0010
    /// ("discarding a stale event is silent by design").
    /// </summary>
    private async Task ApplyProductLineAsync(ProductLine incoming, CancellationToken ct)
    {
        var filter = Builders<ProductLine>.Filter.And(
            Builders<ProductLine>.Filter.Eq(p => p.Code, incoming.Code),
            Builders<ProductLine>.Filter.Lt(p => p.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<ProductLine>.Update
            .Set(p => p.Code, incoming.Code)
            .Set(p => p.Name, incoming.Name)
            .Set(p => p.Tagline, incoming.Tagline)
            .Set(p => p.Countries, incoming.Countries)
            .Set(p => p.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.ProductLines, filter, update, $"ProductLine {incoming.Code}", ct);
    }

    /// <summary>Country: single writer, same shape as ProductLine above. One shared node today (ADR-0017).</summary>
    private async Task ApplyCountryAsync(Country incoming, CancellationToken ct)
    {
        var filter = Builders<Country>.Filter.And(
            Builders<Country>.Filter.Eq(c => c.Id, incoming.Id),
            Builders<Country>.Filter.Lt(c => c.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<Country>.Update
            .Set(c => c.Name, incoming.Name)
            .Set(c => c.Code, incoming.Code)
            .Set(c => c.Description, incoming.Description)
            .Set(c => c.Highlights, incoming.Highlights)
            .Set(c => c.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.Countries, filter, update, $"Country {incoming.Id}", ct);
    }

    /// <summary>Region: single writer, same shape as ProductLine above (ADR-0017).</summary>
    private async Task ApplyRegionAsync(Region incoming, CancellationToken ct)
    {
        var filter = Builders<Region>.Filter.And(
            Builders<Region>.Filter.Eq(r => r.Id, incoming.Id),
            Builders<Region>.Filter.Lt(r => r.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<Region>.Update
            .Set(r => r.Name, incoming.Name)
            .Set(r => r.Slug, incoming.Slug)
            .Set(r => r.Country, incoming.Country)
            .Set(r => r.ProductLine, incoming.ProductLine)
            .Set(r => r.Description, incoming.Description)
            .Set(r => r.Highlights, incoming.Highlights)
            .Set(r => r.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.Regions, filter, update, $"Region {incoming.Id}", ct);
    }

    /// <summary>Destination: single writer, same shape as ProductLine above. Id maps to Mongo's own _id, already uniquely enforced.</summary>
    private async Task ApplyDestinationAsync(Destination incoming, CancellationToken ct)
    {
        var filter = Builders<Destination>.Filter.And(
            Builders<Destination>.Filter.Eq(d => d.Id, incoming.Id),
            Builders<Destination>.Filter.Lt(d => d.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        // No .Set(d => d.Id, ...) here — Id maps to Mongo's own _id
        // (MongoClassMaps), which is immutable on update; the filter's
        // Eq(d => d.Id, ...) above already guarantees it, and Mongo
        // populates _id from that same equality condition on insert.
        var update = Builders<Destination>.Update
            .Set(d => d.Name, incoming.Name)
            .Set(d => d.Slug, incoming.Slug)
            .Set(d => d.Country, incoming.Country)
            .Set(d => d.Region, incoming.Region)
            .Set(d => d.ProductLine, incoming.ProductLine)
            .Set(d => d.Description, incoming.Description)
            .Set(d => d.Latitude, incoming.Latitude)
            .Set(d => d.Longitude, incoming.Longitude)
            .Set(d => d.IncludedPerks, incoming.IncludedPerks)
            .Set(d => d.OptionalAddOns, incoming.OptionalAddOns)
            .Set(d => d.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.Destinations, filter, update, $"Destination {incoming.Id}", ct);
    }

    /// <summary>Accommodation: single writer, same shape as ProductLine above (ADR-0017). Destination/Tags/OfficialRating added in ADR-0019 — Accommodation becomes independently browsable (the Stays search page), not just reachable via Product.</summary>
    private async Task ApplyAccommodationAsync(Accommodation incoming, CancellationToken ct)
    {
        var filter = Builders<Accommodation>.Filter.And(
            Builders<Accommodation>.Filter.Eq(a => a.Id, incoming.Id),
            Builders<Accommodation>.Filter.Lt(a => a.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<Accommodation>.Update
            .Set(a => a.Name, incoming.Name)
            .Set(a => a.Description, incoming.Description)
            .Set(a => a.Highlights, incoming.Highlights)
            .Set(a => a.HeroImageUrl, incoming.HeroImageUrl)
            .Set(a => a.Destination, incoming.Destination)
            .Set(a => a.Type, incoming.Type)
            .Set(a => a.Tags, incoming.Tags)
            .Set(a => a.OfficialRating, incoming.OfficialRating)
            .Set(a => a.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.Accommodations, filter, update, $"Accommodation {incoming.Id}", ct);
    }

    /// <summary>RoomType (ADR-0019): single writer, same shape as ProductLine above. Carries a plain AccommodationId, not an embedded object — see RoomType.cs.</summary>
    private async Task ApplyRoomTypeAsync(RoomType incoming, CancellationToken ct)
    {
        var filter = Builders<RoomType>.Filter.And(
            Builders<RoomType>.Filter.Eq(r => r.Id, incoming.Id),
            Builders<RoomType>.Filter.Lt(r => r.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<RoomType>.Update
            .Set(r => r.Name, incoming.Name)
            .Set(r => r.Description, incoming.Description)
            .Set(r => r.SizeSqm, incoming.SizeSqm)
            .Set(r => r.BedConfiguration, incoming.BedConfiguration)
            .Set(r => r.MaxOccupancy, incoming.MaxOccupancy)
            .Set(r => r.BoardBasis, incoming.BoardBasis)
            .Set(r => r.PriceBands, incoming.PriceBands)
            .Set(r => r.HeroImageUrl, incoming.HeroImageUrl)
            .Set(r => r.MonthlyRatePhp, incoming.MonthlyRatePhp)
            .Set(r => r.AccommodationId, incoming.AccommodationId)
            .Set(r => r.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.RoomTypes, filter, update, $"RoomType {incoming.Id}", ct);
    }

    /// <summary>Activity (ADR-0020): single writer, same shape as ProductLine above. Embeds a full Destination (like Product/Accommodation) since Activities are independently cross-destination browsable.</summary>
    private async Task ApplyActivityAsync(Activity incoming, CancellationToken ct)
    {
        var filter = Builders<Activity>.Filter.And(
            Builders<Activity>.Filter.Eq(a => a.Id, incoming.Id),
            Builders<Activity>.Filter.Lt(a => a.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        var update = Builders<Activity>.Update
            .Set(a => a.Name, incoming.Name)
            .Set(a => a.Description, incoming.Description)
            .Set(a => a.DurationLabel, incoming.DurationLabel)
            .Set(a => a.PricePhp, incoming.PricePhp)
            .Set(a => a.Includes, incoming.Includes)
            .Set(a => a.HeroImageUrl, incoming.HeroImageUrl)
            .Set(a => a.Destination, incoming.Destination)
            .Set(a => a.SourceUpdatedUtc, incoming.SourceUpdatedUtc);

        await UpsertIfNewerAsync(catalog.Activities, filter, update, $"Activity {incoming.Id}", ct);
    }

    /// <summary>
    /// Product: two independent writers (ADR-0014). This handler only
    /// ever $sets catalog fields — never AvailableCount — and gates only
    /// on SourceUpdatedUtc (the catalog fields' own ordering guard).
    /// AvailableCount is seeded to 0 on first insert only
    /// ($setOnInsert — "not yet bookable" until Lakbay.Booking exists,
    /// Phase 4) and is otherwise left exactly as-is, whatever
    /// Lakbay.Booking last wrote.
    /// </summary>
    private async Task ApplyProductAsync(Product incoming, CancellationToken ct)
    {
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, incoming.Id),
            Builders<Product>.Filter.Lt(p => p.SourceUpdatedUtc, incoming.SourceUpdatedUtc));

        // Same reasoning as ApplyDestinationAsync above — no .Set(p => p.Id, ...).
        var update = Builders<Product>.Update
            .Set(p => p.Slug, incoming.Slug)
            .Set(p => p.Name, incoming.Name)
            .Set(p => p.ProductLine, incoming.ProductLine)
            .Set(p => p.Destination, incoming.Destination)
            .Set(p => p.Summary, incoming.Summary)
            .Set(p => p.ItineraryDays, incoming.ItineraryDays)
            .Set(p => p.BoardBasis, incoming.BoardBasis)
            .Set(p => p.PriceBands, incoming.PriceBands)
            .Set(p => p.Accommodation, incoming.Accommodation)
            .Set(p => p.IncludedActivities, incoming.IncludedActivities)
            .Set(p => p.OptionalActivities, incoming.OptionalActivities)
            .Set(p => p.HeroImageUrl, incoming.HeroImageUrl)
            .Set(p => p.SourceUpdatedUtc, incoming.SourceUpdatedUtc)
            .SetOnInsert(p => p.AvailableCount, 0);

        await UpsertIfNewerAsync(catalog.Products, filter, update, $"Product {incoming.Id}", ct);
    }

    private async Task UpsertIfNewerAsync<T>(
        IMongoCollection<T> collection,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        string description,
        CancellationToken ct)
    {
        try
        {
            var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);

            if (result.MatchedCount == 0 && result.UpsertedId is null)
            {
                logger.LogInformation("{Description}: no-op (unexpected — matched nothing and inserted nothing).", description);
            }
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Expected, not a failure: the stored document is already the
            // same age or newer than this event (ADR-0010's guard made
            // the filter not match it), and the upsert's attempted insert
            // collided with the document's own unique key. Silent by
            // design; logged for the discard-rate observability ADR-0010
            // calls for.
            logger.LogInformation("{Description}: discarded a stale/duplicate sync event.", description);
        }
    }
}
