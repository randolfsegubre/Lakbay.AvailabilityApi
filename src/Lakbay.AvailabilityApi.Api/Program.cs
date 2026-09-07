using Lakbay.AvailabilityApi.Api;
using Lakbay.AvailabilityApi.Shared;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Phase 1: real resolvers against MongoDB (Product, ProductLine,
// Destination — matching Lakbay.Contracts' schema). This project contains
// no Service Bus code and never will — event consumption lives entirely
// in the sibling Lakbay.AvailabilityApi.Sync Azure Function (ADR-0009), so
// a burst of availability events never competes with queries here.
MongoClassMaps.Register();

// Bound lazily, inside each factory delegate, resolved from DI's own
// IConfiguration rather than captured once from `builder.Configuration`
// here — found the hard way: WebApplicationFactory (used by
// Lakbay.AvailabilityApi.Tests) layers its test-only config override
// (a different Mongo connection string/database per test) in as part of
// the host build pipeline, which runs *after* this top-level script
// executes. Reading builder.Configuration directly at this point captures
// whatever appsettings.Development.json says — real local Mongo — before
// the test override ever applies, so every test was silently hitting the
// real local database instead of an isolated one. Resolving from
// IServiceProvider's IConfiguration instead reads the final, fully
// layered configuration, at the point each singleton is actually first
// used, by which time the host is completely built.
static MongoDbSettings GetMongoSettings(IServiceProvider sp) =>
    sp.GetRequiredService<IConfiguration>().GetSection("Mongo").Get<MongoDbSettings>()
        ?? throw new InvalidOperationException(
            "Missing \"Mongo\" configuration section (ConnectionString, DatabaseName) — see appsettings.Development.json.");

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(GetMongoSettings(sp).ConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(GetMongoSettings(sp).DatabaseName));
builder.Services.AddSingleton<CatalogContext>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<ProductFilterInputType>();

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

// Phase 1's automatic hand-seed on every real boot was retired once
// Phase 3 made Lakbay.Cms the actual source of truth (see 04_TASKS.md,
// 2026-09-08) — running both left duplicate documents in MongoDB for
// the same real-world holidays under different IDs. CatalogSeeder itself
// is kept, but only as a test-support utility now (see
// Lakbay.AvailabilityApi.Tests' AvailabilityApiFactory) — local dev
// without Lakbay.Cms running no longer has catalog data, on purpose.
app.MapGraphQL();

app.Run();

public partial class Program;
