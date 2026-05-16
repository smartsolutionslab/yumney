using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Telemetry;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Mcp;

/// <summary>
/// Translates an MCP <c>tools/call</c> into an HTTP call against the module
/// endpoint that owns the named capability, forwarding the caller's bearer
/// token. Returns the upstream response body as a <see cref="CallToolResult"/>.
/// </summary>
/// <param name="registry">Discovered capability registry.</param>
/// <param name="httpClientFactory">Factory keyed by Aspire service name.</param>
/// <param name="httpContextAccessor">Source of the caller's bearer token.</param>
/// <param name="logger">Logger for proxy diagnostics.</param>
public sealed partial class RestProxyService(
	AggregatedCapabilityRegistry registry,
	IHttpClientFactory httpClientFactory,
	IHttpContextAccessor httpContextAccessor,
	ILogger<RestProxyService> logger)
{
	/// <summary>Invoke the module endpoint behind <paramref name="toolName"/>.</summary>
	/// <param name="toolName">Capability name from a <c>tools/list</c> response.</param>
	/// <param name="arguments">Tool argument bag (placeholder values + body fields / query params).</param>
	/// <param name="cancellationToken">Cancellation propagated from the SDK.</param>
	/// <returns>Upstream response as a tool result, or an error result on failure.</returns>
	public async Task<CallToolResult> InvokeAsync(string toolName, IDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
	{
		using var activity = McpToolTelemetry.ActivitySource.StartActivity($"mcp.tool/{toolName}", ActivityKind.Client);
		activity?.SetTag("mcp.tool.name", toolName);
		var stopwatch = Stopwatch.StartNew();

		var descriptor = registry.DescriptorForTool(toolName);
		if (descriptor is null)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.UnknownTool, stopwatch.Elapsed.TotalMilliseconds);
			return Error($"Unknown tool '{toolName}'.");
		}

		var serviceName = registry.ServiceNameForTool(toolName);
		if (serviceName is null)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.UnknownTool, stopwatch.Elapsed.TotalMilliseconds);
			return Error($"Tool '{toolName}' is not bound to a discovered service.");
		}

		activity?.SetTag("mcp.tool.service", serviceName);

		var built = RouteUrlBuilder.Build(descriptor.HttpMethod, descriptor.RoutePattern, arguments);
		if (built.MissingPlaceholders.Count > 0)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.MissingArguments, stopwatch.Elapsed.TotalMilliseconds);
			return Error($"Tool '{toolName}' is missing required argument(s): {string.Join(", ", built.MissingPlaceholders)}.");
		}

		var bearer = ExtractBearer();
		if (bearer is null)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.Unauthenticated, stopwatch.Elapsed.TotalMilliseconds);
			return Error("No bearer token forwarded with the MCP request — caller must authenticate.");
		}

		var client = httpClientFactory.CreateClient(serviceName);
		using var request = new HttpRequestMessage(new HttpMethod(descriptor.HttpMethod), built.Url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
		if (built.Body is not null)
		{
			request.Content = new StringContent(built.Body, Encoding.UTF8, "application/json");
		}

		try
		{
			using var response = await client.SendAsync(request, cancellationToken);
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			activity?.SetTag("http.response.status_code", (int)response.StatusCode);
			var outcome = McpToolTelemetry.OutcomeFromHttpStatus((int)response.StatusCode);
			McpToolTelemetry.Record(toolName, outcome, stopwatch.Elapsed.TotalMilliseconds);

			if (!response.IsSuccessStatusCode)
			{
				LogUpstreamFailure(toolName, serviceName, (int)response.StatusCode);
				return Error($"Upstream {serviceName} returned {(int)response.StatusCode}: {body}");
			}

			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = body }],
				IsError = false,
			};
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.Timeout, stopwatch.Elapsed.TotalMilliseconds);
			throw;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			McpToolTelemetry.Record(toolName, McpToolTelemetry.Outcomes.ProxyError, stopwatch.Elapsed.TotalMilliseconds);
			LogProxyException(toolName, serviceName, ex.Message);
			return Error($"Proxy call to {serviceName} failed: {ex.Message}");
		}
	}

	private static CallToolResult Error(string message) => new()
	{
		Content = [new TextContentBlock { Text = message }],
		IsError = true,
	};

	private string? ExtractBearer()
	{
		var header = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
		if (string.IsNullOrEmpty(header)) return null;
		const string prefix = "Bearer ";
		return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			? header[prefix.Length..].Trim()
			: null;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Proxy call for tool {ToolName} to {ServiceName} returned non-success status {StatusCode}.")]
	private partial void LogUpstreamFailure(string toolName, string serviceName, int statusCode);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Proxy call for tool {ToolName} to {ServiceName} threw: {Reason}")]
	private partial void LogProxyException(string toolName, string serviceName, string reason);
}
