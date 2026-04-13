using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Commands;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

/// <summary>
/// Handles Keycloak service account token acquisition with distributed caching.
/// </summary>
#pragma warning disable SA1303, SA1311, SA1601
public sealed partial class KeycloakTokenProvider(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    IDistributedCache cache,
    ILogger<KeycloakTokenProvider> logger)
{
    private const string tokenCacheKey = "keycloak:admin:token";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly TimeSpan tokenExpiryBuffer = TimeSpan.FromSeconds(30);

    public async Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var cachedToken = await cache.GetStringAsync(tokenCacheKey, cancellationToken);
        if (cachedToken.HasValue()) return cachedToken!;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.Value.ClientId,
            ["client_secret"] = options.Value.ClientSecret,
        });

        try
        {
            var tokenUrl = $"/realms/{options.Value.Realm}/protocol/openid-connect/token";
            var response = await httpClient.PostAsync(tokenUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogTokenAcquisitionFailed(response.StatusCode);
                return RegistrationErrors.IdentityProviderUnavailable;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(jsonOptions, cancellationToken);
            var absoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse!.ExpiresIn) - tokenExpiryBuffer;
            await cache.SetStringAsync(
                tokenCacheKey,
                tokenResponse.AccessToken,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow },
                cancellationToken);

            return tokenResponse.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            LogTokenAcquisitionHttpError(ex);
            return RegistrationErrors.IdentityProviderUnavailable;
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to obtain service account token. Status: {StatusCode}")]
    private partial void LogTokenAcquisitionFailed(HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while obtaining service account token")]
    private partial void LogTokenAcquisitionHttpError(Exception ex);
}
#pragma warning restore SA1303, SA1311, SA1601
