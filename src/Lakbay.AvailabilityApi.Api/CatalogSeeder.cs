using Lakbay.Contracts;
using MongoDB.Driver;

namespace Lakbay.AvailabilityApi.Api;

/// <summary>
/// Seeds real Philippine destination/product data — not placeholder
/// lorem — the moment the catalog is empty, matching
/// Lakbay.Docs/docs/02_BUILD_PLAN.md's Phase 1 requirement: at least one
/// real destination per product line, sourced from the Lakbay Blueprint's
/// market research. Runs once at startup; a no-op once seeded.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedIfEmptyAsync(CatalogContext catalog, CancellationToken ct = default)
    {
        var alreadySeeded = await catalog.ProductLines
            .Find(FilterDefinition<ProductLine>.Empty)
            .AnyAsync(ct);

        if (alreadySeeded)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var productLines = new[]
        {
            new ProductLine
            {
                Code = ProductLineCode.Alon,
                Name = "Alon",
                Tagline = "Islands & water adventure",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Amihan,
                Name = "Amihan",
                Tagline = "Highland & cool-climate escapes",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Parul,
                Name = "Parul",
                Tagline = "Festive & light tourism",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Pamana,
                Name = "Pamana",
                Tagline = "Heritage & culture",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
        };
        await catalog.ProductLines.InsertManyAsync(productLines, cancellationToken: ct);

        var coron = new Destination
        {
            Id = "dest-coron",
            Name = "Coron, Palawan",
            Country = "PH",
            Region = "Palawan",
            ProductLine = ProductLineCode.Alon,
            Description = "Island-hopping among limestone karsts, WWII wreck diving, and the Big and Small Lagoons.",
            Latitude = 11.9973,
            Longitude = 120.2046,
            SourceUpdatedUtc = now,
        };
        var baguio = new Destination
        {
            Id = "dest-baguio",
            Name = "Baguio",
            Country = "PH",
            Region = "Cordillera Administrative Region",
            ProductLine = ProductLineCode.Amihan,
            Description = "The Philippines' Summer Capital — pine forests and cool air at 1,540m, without leaving the tropics.",
            Latitude = 16.4023,
            Longitude = 120.5960,
            SourceUpdatedUtc = now,
        };
        var pampanga = new Destination
        {
            Id = "dest-san-fernando-pampanga",
            Name = "San Fernando, Pampanga",
            Country = "PH",
            Region = "Central Luzon",
            ProductLine = ProductLineCode.Parul,
            Description = "The Christmas Capital of the Philippines — home of the Giant Lantern Festival.",
            Latitude = 15.0286,
            Longitude = 120.6898,
            SourceUpdatedUtc = now,
        };
        var vigan = new Destination
        {
            Id = "dest-vigan",
            Name = "Vigan",
            Country = "PH",
            Region = "Ilocos Sur",
            ProductLine = ProductLineCode.Pamana,
            Description = "A UNESCO World Heritage colonial-era town of cobblestone streets and preserved Spanish-era houses.",
            Latitude = 17.5747,
            Longitude = 120.3869,
            SourceUpdatedUtc = now,
        };

        await catalog.Destinations.InsertManyAsync([coron, baguio, pampanga, vigan], cancellationToken: ct);

        var products = new[]
        {
            new Product
            {
                Id = "prod-coron-island-hop-3d2n",
                Slug = "coron-island-hopping-3d2n",
                Name = "Coron Island Hopping, 3 Days 2 Nights",
                ProductLine = ProductLineCode.Alon,
                Destination = coron,
                Summary = "Big Lagoon, Kayangan Lake, and Skeleton Wreck — three days of island-hopping out of Coron town.",
                ItineraryDays = 3,
                BoardBasis = BoardBasis.HalfBoard,
                PriceBands =
                [
                    new PriceBand
                    {
                        Label = "Regular season",
                        StartDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2027, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                        PricePhp = 12500m,
                    },
                ],
                AvailableCount = 8,
                SourceUpdatedUtc = now,
            },
            new Product
            {
                Id = "prod-baguio-cool-weekend-2d1n",
                Slug = "baguio-cool-weekend-2d1n",
                Name = "Baguio Cool-Weather Weekend, 2 Days 1 Night",
                ProductLine = ProductLineCode.Amihan,
                Destination = baguio,
                Summary = "Session Road, Burnham Park, and a Mines View Park sunrise — a short highland escape from Manila heat.",
                ItineraryDays = 2,
                BoardBasis = BoardBasis.Breakfast,
                PriceBands =
                [
                    new PriceBand
                    {
                        Label = "Weekend rate",
                        StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                        PricePhp = 6800m,
                    },
                ],
                AvailableCount = 15,
                SourceUpdatedUtc = now,
            },
            new Product
            {
                Id = "prod-pampanga-lantern-festival-1d",
                Slug = "giant-lantern-festival-day-trip",
                Name = "Giant Lantern Festival Day Trip",
                ProductLine = ProductLineCode.Parul,
                Destination = pampanga,
                Summary = "A guided day trip to San Fernando's Giant Lantern Festival, the Philippines' answer to a light-tourism holiday.",
                ItineraryDays = 1,
                BoardBasis = BoardBasis.RoomOnly,
                PriceBands =
                [
                    new PriceBand
                    {
                        Label = "December season",
                        StartDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2026, 12, 24, 0, 0, 0, DateTimeKind.Utc),
                        PricePhp = 2200m,
                    },
                ],
                AvailableCount = 30,
                SourceUpdatedUtc = now,
            },
            new Product
            {
                Id = "prod-vigan-heritage-walk-2d1n",
                Slug = "vigan-heritage-walk-2d1n",
                Name = "Vigan Heritage Walk, 2 Days 1 Night",
                ProductLine = ProductLineCode.Pamana,
                Destination = vigan,
                Summary = "Calle Crisologo by kalesa, Bantay Bell Tower, and a pottery-making visit in a UNESCO World Heritage town.",
                ItineraryDays = 2,
                BoardBasis = BoardBasis.Breakfast,
                PriceBands =
                [
                    new PriceBand
                    {
                        Label = "Regular season",
                        StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                        PricePhp = 7400m,
                    },
                ],
                AvailableCount = 10,
                SourceUpdatedUtc = now,
            },
        };

        await catalog.Products.InsertManyAsync(products, cancellationToken: ct);
    }
}
