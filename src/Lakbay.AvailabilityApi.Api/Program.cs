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

// Lakbay.Web calls this API cross-origin (localhost:3000 -> localhost:5000
// in dev; different subdomains/hosts in every real environment). The
// allowed origin list comes from config so it's not hard-coded per
// environment — see appsettings.Development.json for the local default.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGraphQL();

app.Run();

public partial class Program;
