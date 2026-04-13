using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SmartSolutionsLab.Yumney.Shared.Web.HealthChecks;

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var latency = await db.PingAsync();

            return latency.TotalMilliseconds < 1000
                ? HealthCheckResult.Healthy($"Redis ping: {latency.TotalMilliseconds:F0}ms")
                : HealthCheckResult.Degraded($"Redis ping slow: {latency.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}
