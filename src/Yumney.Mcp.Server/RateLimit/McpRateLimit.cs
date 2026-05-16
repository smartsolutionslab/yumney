using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;
using StackExchange.Redis;

namespace SmartSolutionsLab.Yumney.Mcp.Server.RateLimit;

/// <summary>
/// Sliding-window Redis-backed rate-limit policy applied to <c>/mcp</c>.
/// Public MCP means public load — without per-user limits a runaway LLM
/// session could fan out tool calls at the model's max rate and drown
/// the downstream module APIs.
/// </summary>
public static class McpRateLimit
{
	/// <summary>Policy name registered with the rate-limiter and applied to the MCP route.</summary>
	public const string PolicyName = "Mcp";

	/// <summary>Per-user ceiling. Starting value — revise against the `mcp.rate_limit.rejections` counter.</summary>
	public const int PermitLimit = 60;

	/// <summary>Meter name for telemetry exporters.</summary>
	public const string MeterName = "Yumney.Mcp.Server.RateLimit";

	/// <summary>Window length matching <see cref="PermitLimit"/>.</summary>
	public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

#pragma warning disable SA1303, SA1311
	private static readonly Meter meter = new(MeterName);
	private static readonly Counter<long> rejections = meter.CreateCounter<long>(
		name: "mcp.rate_limit.rejections",
		unit: "{request}",
		description: "Number of MCP requests rejected by the rate limiter.");
#pragma warning restore SA1303, SA1311

	/// <summary>
	/// Registers the MCP policy on <paramref name="options"/>. Skips real
	/// limiting when <paramref name="isE2ETests"/> so the Playwright suite
	/// doesn't trip on burst patterns it generates intentionally.
	/// </summary>
	/// <param name="options">Rate-limiter options builder.</param>
	/// <param name="isE2ETests">True when running under the E2E harness.</param>
	public static void AddMcpPolicy(this RateLimiterOptions options, bool isE2ETests)
	{
		options.AddPolicy(PolicyName, context =>
		{
			if (isE2ETests)
			{
				return RateLimitPartition.GetNoLimiter("e2e-tests");
			}

			var userId = PartitionKey(context);
			return RedisRateLimitPartition.GetSlidingWindowRateLimiter(userId, _ => new RedisSlidingWindowRateLimiterOptions
			{
				PermitLimit = PermitLimit,
				Window = Window,
				ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
			});
		});

		options.OnRejected = async (rejectionContext, cancellationToken) =>
		{
			rejections.Add(1);
			var response = rejectionContext.HttpContext.Response;
			response.StatusCode = StatusCodes.Status429TooManyRequests;
			response.Headers.RetryAfter = ((int)Window.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
			response.ContentType = "application/json";

			// Phrased so the LLM can read the body and back off without
			// further prompting — `retry_after_seconds` is the actionable bit.
			var body = JsonSerializer.Serialize(new
			{
				error = "rate_limit_exceeded",
				message = $"Yumney MCP allows up to {PermitLimit} tool calls per minute per user. Wait and retry.",
				retry_after_seconds = (int)Window.TotalSeconds,
			});
			await response.WriteAsync(body, cancellationToken);
		};
	}

	/// <summary>Resolves the partition key — subject claim, then remote IP, then "anonymous".</summary>
	/// <param name="context">Inbound request.</param>
	/// <returns>Stable per-user/per-IP key.</returns>
	public static string PartitionKey(HttpContext context) =>
		context.User?.FindFirstValue("sub")
			?? context.Connection.RemoteIpAddress?.ToString()
			?? "anonymous";
}
