using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartSolutionsLab.Yumney.Shared.Web.HealthChecks;

public sealed class KeycloakHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var realmUrl = KeycloakDefaults.RealmUrl(configuration);
        var discoveryUrl = $"{realmUrl}/.well-known/openid-configuration";

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(discoveryUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Keycloak realm discovery is reachable")
                : HealthCheckResult.Unhealthy($"Keycloak returned {response.StatusCode}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Keycloak is unreachable", ex);
        }
    }
}
