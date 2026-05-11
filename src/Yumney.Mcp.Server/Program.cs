using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Mcp.Server.Auth;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using SmartSolutionsLab.Yumney.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Match the module hosts' response shape: enums (including CapabilitySurface)
// ship as strings so /discovered-capabilities is round-trippable through the
// same JsonStringEnumConverter convention used by every other Yumney host.
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddKeycloakBearerAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<RestProxyService>();

builder.Services.AddMcpServer()
	.WithHttpTransport()
	.WithListToolsHandler(CapabilityToolRegistration.ListToolsAsync)
	.WithCallToolHandler(async (context, cancellationToken) =>
	{
		var proxy = context.Services!.GetRequiredService<RestProxyService>();
		var name = context.Params?.Name ?? "<unknown>";
		return await proxy.InvokeAsync(name, context.Params?.Arguments, cancellationToken);
	});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
})).AllowAnonymous();

// MCP HTTP/SSE transport at /mcp. External clients (Claude Desktop, custom GPTs)
// authenticate via the standard Authorization: Bearer header — the bearer is
// forwarded to the module endpoint by RestProxyService when the LLM calls a tool.
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
