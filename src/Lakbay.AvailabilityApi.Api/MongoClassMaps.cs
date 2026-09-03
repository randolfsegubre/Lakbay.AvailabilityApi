using Lakbay.Contracts;
using MongoDB.Bson.Serialization;

namespace Lakbay.AvailabilityApi.Api;

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

        if (!BsonClassMap.IsClassMapRegistered(typeof(Destination)))
        {
            BsonClassMap.RegisterClassMap<Destination>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Product)))
        {
            BsonClassMap.RegisterClassMap<Product>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
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
            // and Product don't need this: their Id *is* mapped to _id).
            BsonClassMap.RegisterClassMap<ProductLine>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
