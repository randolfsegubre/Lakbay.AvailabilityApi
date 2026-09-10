using Lakbay.AvailabilityApi.Shared;
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
                Name = "Islands",
                Tagline = "Islands & water adventure",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Amihan,
                Name = "Highlands",
                Tagline = "Highland & cool-climate escapes",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Parul,
                Name = "Festivals",
                Tagline = "Festive & light tourism",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
            new ProductLine
            {
                Code = ProductLineCode.Pamana,
                Name = "Heritage",
                Tagline = "Heritage & culture",
                Countries = ["PH"],
                SourceUpdatedUtc = now,
            },
        };
        await catalog.ProductLines.InsertManyAsync(productLines, cancellationToken: ct);

        // ADR-0017: Country/Region are real entities now, not flat
        // strings — a single shared Country (Lakbay is one country
        // today) plus one Region per ProductLine, matching the real
        // Cms seeder's shape so this test fixture stays honest.
        var country = new Country
        {
            Id = "country-ph",
            Name = "Philippines",
            Code = "PH",
            Description = "An archipelago of over 7,000 islands in Southeast Asia.",
            Highlights = ["Tropical climate year-round", "English widely spoken", "Currency: Philippine Peso (PHP)"],
            SourceUpdatedUtc = now,
        };
        await catalog.Countries.InsertOneAsync(country, cancellationToken: ct);

        var palawan = new Region
        {
            Id = "region-palawan",
            Name = "Palawan",
            Slug = "palawan",
            Country = country,
            ProductLine = ProductLineCode.Alon,
            Description = "The Philippines' last ecological frontier.",
            Highlights = ["Regularly ranked among the world's best islands"],
            SourceUpdatedUtc = now,
        };
        var cordilleraAmihan = new Region
        {
            Id = "region-cordillera-amihan",
            Name = "Cordillera Administrative Region",
            Slug = "cordillera-amihan",
            Country = country,
            ProductLine = ProductLineCode.Amihan,
            Description = "A mountainous region in northern Luzon.",
            Highlights = ["Several degrees cooler than the lowlands year-round"],
            SourceUpdatedUtc = now,
        };
        var centralLuzon = new Region
        {
            Id = "region-central-luzon",
            Name = "Central Luzon",
            Slug = "central-luzon",
            Country = country,
            ProductLine = ProductLineCode.Parul,
            Description = "Home to the Giant Lantern Festival.",
            Highlights = ["San Fernando is the Christmas Capital of the Philippines"],
            SourceUpdatedUtc = now,
        };
        var ilocosPamana = new Region
        {
            Id = "region-ilocos-pamana",
            Name = "Ilocos Region",
            Slug = "ilocos-pamana",
            Country = country,
            ProductLine = ProductLineCode.Pamana,
            Description = "The Philippines' best-preserved Spanish colonial town.",
            Highlights = ["A UNESCO World Heritage Site"],
            SourceUpdatedUtc = now,
        };
        await catalog.Regions.InsertManyAsync([palawan, cordilleraAmihan, centralLuzon, ilocosPamana], cancellationToken: ct);

        var coron = new Destination
        {
            Id = "dest-coron",
            Name = "Coron, Palawan",
            Slug = "coron",
            Country = "PH",
            Region = palawan,
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
            Slug = "baguio",
            Country = "PH",
            Region = cordilleraAmihan,
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
            Slug = "san-fernando-pampanga",
            Country = "PH",
            Region = centralLuzon,
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
            Slug = "vigan",
            Country = "PH",
            Region = ilocosPamana,
            ProductLine = ProductLineCode.Pamana,
            Description = "A UNESCO World Heritage colonial-era town of cobblestone streets and preserved Spanish-era houses.",
            Latitude = 17.5747,
            Longitude = 120.3869,
            SourceUpdatedUtc = now,
        };

        await catalog.Destinations.InsertManyAsync([coron, baguio, pampanga, vigan], cancellationToken: ct);

        // ADR-0017: Accommodation is a real entity now, not two flat
        // strings on Product.
        var coronBaysideInn = new Accommodation
        {
            Id = "accom-coron-bayside-inn",
            Name = "Coron Bayside Inn",
            Description = "A simple harborside inn a five-minute walk from the public market and the town's island-hopping jetty.",
            Highlights = ["Walking distance to the town's main strip of dive shops and restaurants"],
            Destination = coron,
            Type = AccommodationType.PensionHouse,
            Tags = ["Budget-Friendly", "Convenient Location"],
            SourceUpdatedUtc = now,
        };
        var sessionRoadPineHouse = new Accommodation
        {
            Id = "accom-session-road-pine-house",
            Name = "Session Road Pine House",
            Description = "A short tricycle ride from Burnham Park, with a fireplace lounge for Baguio's cool evenings.",
            Highlights = ["Fireplace lounge open to guests on cool Baguio evenings"],
            Destination = baguio,
            Type = AccommodationType.Apartel,
            Tags = ["Family-Friendly", "Cozy"],
            OfficialRating = 3.0,
            SourceUpdatedUtc = now,
        };
        var lanternCityHomestay = new Accommodation
        {
            Id = "accom-lantern-city-homestay",
            Name = "Lantern City Homestay",
            Description = "A homestay in San Fernando's lantern-making district, footsteps from the festival grounds.",
            Highlights = ["Run by a local family in San Fernando's lantern-making district"],
            Destination = pampanga,
            Type = AccommodationType.Homestay,
            Tags = ["Homestay", "Budget-Friendly"],
            SourceUpdatedUtc = now,
        };
        var calleCrisologoHeritageHouse = new Accommodation
        {
            Id = "accom-calle-crisologo-heritage-house",
            Name = "Calle Crisologo Heritage House",
            Description = "A restored ancestral house on Calle Crisologo's cobblestone strip, steps from the kalesa stand.",
            Highlights = ["A genuinely restored 19th-century ancestral house, not a modern replica"],
            Destination = vigan,
            Type = AccommodationType.VacationRental,
            Tags = ["Heritage Stay", "Boutique"],
            OfficialRating = 3.5,
            SourceUpdatedUtc = now,
        };
        await catalog.Accommodations.InsertManyAsync(
            [coronBaysideInn, sessionRoadPineHouse, lanternCityHomestay, calleCrisologoHeritageHouse], cancellationToken: ct);

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
                Accommodation = coronBaysideInn,
                IncludedActivities = ["Big Lagoon and Kayangan Lake island-hopping boat tour", "Skeleton Wreck snorkeling stop"],
                OptionalActivities = ["Twin-tank wreck diving for certified divers"],
                HeroImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Kayangan%20Lake%2C%20Coron%20-%20Palawan.jpg",
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
                Accommodation = sessionRoadPineHouse,
                IncludedActivities = ["Guided Burnham Park & Session Road walking tour", "Mines View Park sunrise viewing"],
                OptionalActivities = ["Strawberry picking day trip to La Trinidad"],
                HeroImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Baguio%20City%2C%20Philippines(landscape%20view).jpg",
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
                Accommodation = lanternCityHomestay,
                IncludedActivities = ["Guided Giant Lantern Festival viewing", "Lantern-making workshop visit"],
                OptionalActivities = ["Pampanga culinary trail (sisig, Susie's cuisine)"],
                HeroImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/WV%20banner%20San%20Fernando%20Pampanga%20giant%20Christmas%20lanterns.JPG",
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
                Accommodation = calleCrisologoHeritageHouse,
                IncludedActivities = ["Kalesa (horse-cart) heritage tour", "Bantay Bell Tower visit"],
                OptionalActivities = ["Longganisa & bagnet cooking class"],
                HeroImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/The%20Calle%20Crisologo%20in%20Vigan%2C%20Ilocos%20Sur.jpg",
                SourceUpdatedUtc = now,
            },
        };

        await catalog.Products.InsertManyAsync(products, cancellationToken: ct);
    }
}
