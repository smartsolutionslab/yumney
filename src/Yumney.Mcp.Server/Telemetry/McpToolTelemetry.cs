using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Telemetry;

/// <summary>
/// OpenTelemetry meter + activity source for MCP tool invocations.
/// Two signals power the Phase-3 dashboards:
/// <c>mcp.tool.invocations</c> (counter tagged with tool name + outcome) and
/// <c>mcp.tool.duration</c> (histogram tagged with tool name).
/// Each tool call also gets a span named <c>mcp.tool/{toolName}</c> so the
/// trace links the inbound MCP request to the upstream module HTTP call
/// (W3C traceparent is forwarded by the HttpClient handler chain).
/// </summary>
public static class McpToolTelemetry
{
	/// <summary>Meter name registered with OpenTelemetry exporters.</summary>
	public const string MeterName = "Yumney.Mcp.Server.Tools";

	/// <summary>Activity source name registered with OpenTelemetry exporters.</summary>
	public const string ActivitySourceName = "Yumney.Mcp.Server.Tools";

	/// <summary>ActivitySource exposed so the proxy can <c>StartActivity</c> per call.</summary>
	public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

#pragma warning disable SA1303, SA1311
	private static readonly Meter meter = new(MeterName);
	private static readonly Counter<long> invocations = meter.CreateCounter<long>(
		name: "mcp.tool.invocations",
		unit: "{invocation}",
		description: "MCP tool invocations, tagged with tool name and outcome.");

	private static readonly Histogram<double> duration = meter.CreateHistogram<double>(
		name: "mcp.tool.duration",
		unit: "ms",
		description: "Wall-clock duration of an MCP tool invocation, tagged with tool name.");
#pragma warning restore SA1303, SA1311

	/// <summary>Outcome tag values — keep the set small so the cardinality stays useful for alerting.</summary>
	public static class Outcomes
	{
		public const string Success = "success";
		public const string ClientError = "client_error";
		public const string ServerError = "server_error";
		public const string Timeout = "timeout";
		public const string UnknownTool = "unknown_tool";
		public const string MissingArguments = "missing_arguments";
		public const string Unauthenticated = "unauthenticated";
		public const string ProxyError = "proxy_error";
	}

	/// <summary>Records the outcome + elapsed time for one invocation.</summary>
	/// <param name="toolName">Capability name (matches the MCP tool identifier).</param>
	/// <param name="outcome">One of <see cref="Outcomes"/>.</param>
	/// <param name="elapsedMilliseconds">Total wall-clock time for the invocation.</param>
	public static void Record(string toolName, string outcome, double elapsedMilliseconds)
	{
		invocations.Add(1, new KeyValuePair<string, object?>("tool", toolName), new KeyValuePair<string, object?>("outcome", outcome));
		duration.Record(elapsedMilliseconds, new KeyValuePair<string, object?>("tool", toolName));
	}

	/// <summary>Maps an upstream HTTP status code to one of the metric outcome values.</summary>
	/// <param name="statusCode">HTTP status code from the upstream module response.</param>
	/// <returns>Outcome string for the metrics tag.</returns>
	public static string OutcomeFromHttpStatus(int statusCode) => statusCode switch
	{
		>= 200 and < 300 => Outcomes.Success,
		>= 400 and < 500 => Outcomes.ClientError,
		_ => Outcomes.ServerError,
	};
}
