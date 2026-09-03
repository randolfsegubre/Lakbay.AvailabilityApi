using Lakbay.AvailabilityApi.Api;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Phase 1: real resolvers against MongoDB (Product, ProductLine,
// Destination — matching Lakbay.Contracts' schema). This project contains
// no Service Bus code and never will — event consumption lives entirely
// in the sibling Lakbay.AvailabilityApi.Sync Azure Function (ADR-0009), so
// a burst of availability events never competes with queries here.
MongoClassMaps.Register();

var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoDbSettings>()
    ?? throw new InvalidOperationException(
        "Missing \"Mongo\" configuration section (ConnectionString, DatabaseName) — see appsettings.Development.json.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));
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

await CatalogSeeder.SeedIfEmptyAsync(app.Services.GetRequiredService<CatalogContext>());

app.MapGraphQL();

app.Run();

public partial class Program;
