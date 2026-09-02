using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

builder.Build().Run();
