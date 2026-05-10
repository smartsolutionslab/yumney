using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
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

builder.Services.AddMcpServer()
	.WithHttpTransport()
	.WithListToolsHandler(CapabilityToolRegistration.ListToolsAsync)
	.WithCallToolHandler(CapabilityToolRegistration.CallToolAsync);

var app = builder.Build();

app.MapDefaultEndpoints();

// Debug endpoint: dump everything the discovery service has found so we can
// verify the cross-host pipeline + MCP-surface filtering at a glance without
// speaking the full MCP protocol.
app.MapGet("/discovered-capabilities", (AggregatedCapabilityRegistry registry) => Results.Ok(new
{
	serviceCount = registry.Manifests.Count,
	capabilityCount = registry.CapabilityCount,
	mcpToolCount = CapabilityToolRegistration.BuildTools(registry).Count,
	services = registry.Manifests.Keys.Order().ToArray(),
	capabilities = registry.AllCapabilities(),
}));

// MCP HTTP/SSE transport at /mcp. External clients (Claude Desktop, custom GPTs)
// connect here once OAuth ships in Phase 4c.
app.MapMcp("/mcp");

app.Run();
