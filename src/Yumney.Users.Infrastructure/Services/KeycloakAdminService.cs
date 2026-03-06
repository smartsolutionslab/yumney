using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Yumney.Shared.Common;
using Yumney.Users.Application.Commands;
using Yumney.Users.Application.Interfaces;

namespace Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase for private fields)
public sealed class KeycloakAdminService(HttpClient httpClient, ILogger<KeycloakAdminService> logger)
    : IKeycloakAdminService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<string>> CreateUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
            return Result<string>.Failure(token.Error!);
        }

        return await CreateKeycloakUserAsync(email, password, displayName, token.Value, cancellationToken);
    }

    private async Task<Result<string>> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "yumney-api",
            ["client_secret"] = "yumney-api-secret",
        });

        try
        {
            var response = await httpClient.PostAsync("/realms/yumney/protocol/openid-connect/token", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to obtain service account token. Status: {StatusCode}", response.StatusCode);
                return Result<string>.Failure(RegistrationErrors.IdentityProviderUnavailable);
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(jsonOptions, cancellationToken);

            return Result<string>.Success(tokenResponse!.AccessToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while obtaining service account token");
            return Result<string>.Failure(RegistrationErrors.IdentityProviderUnavailable);
        }
    }

    private async Task<Result<string>> CreateKeycloakUserAsync(
        string email,
        string password,
        string displayName,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var userPayload = new
        {
            username = email,
            email,
            enabled = true,
            emailVerified = false,
            firstName = displayName,
            requiredActions = new[] { "VERIFY_EMAIL" },
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = password,
                    temporary = false,
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/realms/yumney/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(userPayload, options: jsonOptions);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return Result<string>.Failure(RegistrationErrors.EmailAlreadyExists);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to create Keycloak user. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return Result<string>.Failure(RegistrationErrors.UserCreationFailed);
            }

            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
            {
                logger.LogError("Keycloak did not return a Location header after user creation");
                return Result<string>.Failure(RegistrationErrors.UserCreationFailed);
            }

            var keycloakUserId = locationHeader.Split('/').Last();
            return Result<string>.Success(keycloakUserId);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while creating Keycloak user");
            return Result<string>.Failure(RegistrationErrors.IdentityProviderUnavailable);
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
