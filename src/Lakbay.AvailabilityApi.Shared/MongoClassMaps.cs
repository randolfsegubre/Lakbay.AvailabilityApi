using Lakbay.Contracts;
using MongoDB.Bson.Serialization;

namespace Lakbay.AvailabilityApi.Shared;

/// <summary>
/// Maps Lakbay.Contracts' plain POCOs onto MongoDB documents without
/// putting any Mongo-specific attribute on the shared Contracts types
/// themselves — an Adapter between the shared contract and this repo's
/// storage choice (see
/// Lakbay.Docs/docs/03_ARCHITECTURE_AND_PATTERNS_GUIDE.md). Call once at
/// startup; guarded so re-registration in the same process (e.g. several
/// WebApplicationFactory instances across test classes) doesn't throw.
/// </summary>
public static class MongoClassMaps
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(PriceBand)))
        {
            BsonClassMap.RegisterClassMap<PriceBand>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Country)))
        {
            BsonClassMap.RegisterClassMap<Country>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Region)))
        {
            BsonClassMap.RegisterClassMap<Region>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Accommodation)))
        {
            BsonClassMap.RegisterClassMap<Accommodation>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(RoomType)))
        {
            BsonClassMap.RegisterClassMap<RoomType>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Activity)))
        {
            BsonClassMap.RegisterClassMap<Activity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Destination)))
        {
            BsonClassMap.RegisterClassMap<Destination>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                // ADR-0014: defensive, same reasoning as ProductLine below
                // — keeps the door open for a future Mongo-only field
                // (mirroring Product's availabilityUpdatedUtc) without a
                // deserialization break, even though Destination has no
                // second writer today.
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Product)))
        {
            BsonClassMap.RegisterClassMap<Product>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                // ADR-0014: Product's Mongo document carries an internal-
                // only `availabilityUpdatedUtc` field, written by
                // Lakbay.AvailabilityApi.Sync and never mapped onto the
                // shared Lakbay.Contracts.Product record. Without this,
                // BsonClassMap throws the moment it deserializes a
                // document with that extra field — exactly the bug
                // already caught once for ProductLine's auto-assigned
                // _id, below.
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ProductLine)))
        {
            // No natural Id property on ProductLine (its stable key is
            // Code, an enum) — let Mongo assign its own _id and query by
            // Code directly rather than forcing an artificial string id
            // onto a type that doesn't have one in the schema.
            // IgnoreExtraElements is required here specifically because
            // that auto-assigned _id has no matching C# member — without
            // this, BsonClassMap throws on deserialization the moment it
            // sees a document field with nothing to map it to (Destination
            // and Product don't need this for the same reason: their Id
            // *is* mapped to _id — but see ADR-0014 for why Product also
            // sets it now, for a different reason).
            BsonClassMap.RegisterClassMap<ProductLine>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
