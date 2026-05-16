using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Mcp.Server.Auth;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using SmartSolutionsLab.Yumney.Mcp.Server.OpenApi;
using SmartSolutionsLab.Yumney.Mcp.Server.RateLimit;
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

builder.AddRedisClient("redis");
var isE2ETests = builder.Configuration.GetValue<bool>("E2ETests");
builder.Services.AddRateLimiter(options => options.AddMcpPolicy(isE2ETests));

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
app.UseRateLimiter();

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

// RFC 9728 OAuth Protected Resource Metadata. Lets MCP clients (Claude.ai,
// Claude Desktop, ChatGPT, …) discover the Keycloak realm without anyone
// hand-configuring URLs. Optional config override `McpServer:PublicUrl` is
// required in deployments where the request reaches the server through a
// gateway (the Host header is the internal container name, not the public URL).
app.MapOAuthProtectedResourceEndpoint(builder.Configuration.GetValue<string>("McpServer:PublicUrl"));

// OpenAPI 3.1 mirror of the MCP-exposed capability surface. Targets the
// ChatGPT Custom GPT Action path; the discovered capabilities here are the
// same set RestProxyService can invoke.
app.MapGet("/openapi/v1.json", (AggregatedCapabilityRegistry registry, IConfiguration configuration) =>
{
	var serverUrl = configuration.GetValue<string>("McpServer:GatewayUrl") ?? "http://localhost:5100";
	var authorizationUrl = configuration.GetValue<string>("McpServer:KeycloakAuthorizationUrl") ?? $"{KeycloakAuthExtensions.ResolveRealmUrl(configuration)}/protocol/openid-connect/auth";
	var tokenUrl = configuration.GetValue<string>("McpServer:KeycloakTokenUrl") ?? $"{KeycloakAuthExtensions.ResolveRealmUrl(configuration)}/protocol/openid-connect/token";
	return Results.Json(OpenApiCapabilityBuilder.Build(registry, serverUrl, authorizationUrl, tokenUrl));
}).AllowAnonymous();

// MCP HTTP/SSE transport at /mcp. External clients (Claude Desktop, custom GPTs)
// authenticate via the standard Authorization: Bearer header — the bearer is
// forwarded to the module endpoint by RestProxyService when the LLM calls a tool.
app.MapMcp("/mcp").RequireAuthorization().RequireRateLimiting(McpRateLimit.PolicyName);

app.Run();
