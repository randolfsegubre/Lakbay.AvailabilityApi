using Lakbay.AvailabilityApi.Api;

var builder = WebApplication.CreateBuilder(args);

// Phase 0: HotChocolate boots and introspection works. Real resolvers
// against MongoDB (Product, ProductLine, Destination — matching
// Lakbay.Contracts' schema) land in Phase 1. This project contains no
// Service Bus code and never will — event consumption lives entirely in
// the sibling Lakbay.AvailabilityApi.Sync Azure Function (ADR-0009), so
// a burst of availability events never competes with queries here.
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public partial class Program;
