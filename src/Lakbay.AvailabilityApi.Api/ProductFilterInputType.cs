using Lakbay.Contracts;
using HotChocolate.Types;

namespace Lakbay.AvailabilityApi.Api;

/// <summary>
/// Names the GraphQL input type "ProductFilter", matching
/// Lakbay.Contracts' schema exactly. HotChocolate's default convention
/// would otherwise call it "ProductFilterInput" (it appends "Input" to
/// any type it infers as an input object) — invisible when a client
/// embeds filter values as an inline literal in the query text, but a
/// hard failure the moment a client declares it as a proper `$filter:
/// ProductFilter` variable, which is what Lakbay.Web actually does. This
/// is exactly the class of drift ADR-0007's planned schema-diff CI check
/// exists to catch — caught here manually via a real client instead.
/// An Adapter, same shape as MongoClassMaps: adjusts how this repo
/// exposes a Contracts type without putting a HotChocolate attribute on
/// the shared type itself.
/// </summary>
public sealed class ProductFilterInputType : InputObjectType<ProductFilter>
{
    protected override void Configure(IInputObjectTypeDescriptor<ProductFilter> descriptor)
    {
        descriptor.Name("ProductFilter");
    }
}
