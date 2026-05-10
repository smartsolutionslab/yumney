using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register an HttpClient per known module host. Service discovery resolves
// the http://<service-name> base address via Aspire.
foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
{
	builder.Services.AddHttpClient(serviceName, client => client.BaseAddress = new Uri($"http://{serviceName}"))
		.AddServiceDiscovery()
		.AddStandardResilienceHandler();
}

builder.Services.AddSingleton<AggregatedCapabilityRegistry>();
builder.Services.AddHostedService<CapabilityDiscoveryService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Phase 4a debug endpoint: dump everything the discovery service has found so
// we can verify the cross-host pipeline works before adding the MCP protocol
// surface in Phase 4b.
app.MapGet("/discovered-capabilities", (AggregatedCapabilityRegistry registry) => Results.Ok(new
{
	serviceCount = registry.Manifests.Count,
	capabilityCount = registry.CapabilityCount,
	services = registry.Manifests.Keys.Order().ToArray(),
	capabilities = registry.AllCapabilities(),
}));

app.Run();
