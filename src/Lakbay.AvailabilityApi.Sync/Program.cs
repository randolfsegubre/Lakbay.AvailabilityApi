using Azure.Monitor.OpenTelemetry.Exporter;
using Lakbay.AvailabilityApi.Shared;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OpenTelemetry;

// Service-Bus-triggered only (ADR-0009) — no HTTP triggers in this
// project. FunctionsApplication.CreateBuilder already configures worker
// defaults; ConfigureFunctionsWebApplication()/ConfigureFunctionsWorkerDefaults()
// are for the older HostBuilder-based pattern and don't apply here.
var builder = FunctionsApplication.CreateBuilder(args);

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Same Mongo access layer the query API uses (ADR-0009/ADR-0013) — one
// CatalogContext/MongoClassMaps shared via Lakbay.AvailabilityApi.Shared,
// not a second copy that can drift.
MongoClassMaps.Register();

var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoDbSettings>()
    ?? throw new InvalidOperationException(
        "Missing \"Mongo\" configuration section (ConnectionString, DatabaseName) — see local.settings.json.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));
builder.Services.AddSingleton<CatalogContext>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CatalogContext>().EnsureIndexesAsync();
}

host.Run();
