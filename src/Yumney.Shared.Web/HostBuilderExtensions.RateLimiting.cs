using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedisRateLimiting;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using StackExchange.Redis;

namespace SmartSolutionsLab.Yumney.Shared.Web;

/// <content>
/// Per-policy rate-limit wiring. Three policies share the same Redis-backed
/// partitioning shape but differ in algorithm (sliding/fixed window) and
/// per-user vs per-IP key derivation.
/// </content>
public static partial class HostBuilderExtensions
{
	private static WebApplicationBuilder AddYumneyRateLimiting(this WebApplicationBuilder builder)
	{
		var isE2ETests = builder.Configuration.GetValue<bool>("E2ETests");
		builder.Services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.AddRecipeImportPolicy(isE2ETests);
			options.AddGeneralApiPolicy(isE2ETests);
			options.AddAnonymousAuthPolicy(isE2ETests);
		});
		return builder;
	}

	private static void AddRecipeImportPolicy(this RateLimiterOptions options, bool isE2ETests) =>
		options.AddPolicy(RateLimitPolicies.RecipeImport, context =>
		{
			if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

			var userId = context.User?.FindFirstValue(KeycloakClaimTypes.Subject) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
			return RedisRateLimitPartition.GetSlidingWindowRateLimiter(userId, _ => new RedisSlidingWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromMinutes(1),
				ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
			});
		});

	private static void AddGeneralApiPolicy(this RateLimiterOptions options, bool isE2ETests) =>
		options.AddPolicy(RateLimitPolicies.GeneralApi, context =>
		{
			if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

			var userId = context.User?.FindFirstValue(KeycloakClaimTypes.Subject) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
			return RedisRateLimitPartition.GetFixedWindowRateLimiter(userId, _ => new RedisFixedWindowRateLimiterOptions
			{
				PermitLimit = 60,
				Window = TimeSpan.FromMinutes(1),
				ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
			});
		});

	// Anonymous-only — partitions by client IP (resolved via ForwardedHeaders).
	// If RemoteIpAddress is somehow null, the partition key is the literal
	// "unknown" — those requests share a single global bucket of 10/min,
	// which still caps abuse from any path that bypasses the gateway.
	private static void AddAnonymousAuthPolicy(this RateLimiterOptions options, bool isE2ETests) =>
		options.AddPolicy(RateLimitPolicies.AnonymousAuth, context =>
		{
			if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

			var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
			return RedisRateLimitPartition.GetSlidingWindowRateLimiter(clientIp, _ => new RedisSlidingWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromMinutes(1),
				ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
			});
		});
}
