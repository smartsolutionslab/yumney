using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1303 // Const field names should begin with upper-case letter (editorconfig requires camelCase for private fields)
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase for private fields)
public sealed class KeycloakAdminService(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    IDistributedCache cache,
    ILogger<KeycloakAdminService> logger)
    : IKeycloakAdminService
{
    private const string tokenCacheKey = "keycloak:admin:token";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly TimeSpan tokenExpiryBuffer = TimeSpan.FromSeconds(30);
    private static readonly string[] verifyEmailActions = ["VERIFY_EMAIL"];

    public async Task<Result<KeycloakUserId>> CreateUserAsync(
        Email email,
        Password password,
        DisplayName displayName,
        CancellationToken cancellationToken = default)
    {
        using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.create_user");
        activity?.SetTag("keycloak.email", email.Value);

        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Token acquisition failed");
            return Result<KeycloakUserId>.Failure(token.Error!);
        }

        var result = await CreateKeycloakUserAsync(email, password, displayName, token.Value, cancellationToken);
        activity?.SetStatus(result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Error?.Message);
        return result;
    }

    public async Task<Result<KeycloakUserId>> FindUserByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.find_user");
        activity?.SetTag("keycloak.email", email.Value);

        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Token acquisition failed");
            return Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable);
        }

        var encodedEmail = Uri.EscapeDataString(email.Value);
        var url = $"/admin/realms/{options.Value.Realm}/users?email={encodedEmail}&exact=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to search Keycloak users. Status: {StatusCode}", response.StatusCode);
                return Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable);
            }

            var users = await response.Content.ReadFromJsonAsync<KeycloakUserRepresentation[]>(jsonOptions, cancellationToken);

            if (users is null || users.Length == 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "User not found");
                return Result<KeycloakUserId>.Failure(VerificationErrors.UserNotFound);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result<KeycloakUserId>.Success(KeycloakUserId.From(users[0].Id));
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "HTTP error while searching Keycloak users");
            return Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable);
        }
    }

    public async Task<Result> SendVerificationEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
    {
        using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.send_verification_email");

        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Token acquisition failed");
            return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
        }

        var url = $"/admin/realms/{options.Value.Realm}/users/{keycloakUserId.Value}/execute-actions-email";

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        request.Content = JsonContent.Create(verifyEmailActions, options: jsonOptions);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
                logger.LogError("Failed to send verification email. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return Result.Failure(VerificationErrors.SendFailed);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "HTTP error while sending verification email");
            return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
        }
    }

    private static object BuildUserPayload(Email email, Password password, DisplayName displayName)
    {
        return new
        {
            username = email.Value,
            email = email.Value,
            enabled = true,
            emailVerified = false,
            firstName = displayName.Value,
            requiredActions = new[] { "VERIFY_EMAIL" },
            credentials = new[]
            {
                new { type = "password", value = password.Value, temporary = false },
            },
        };
    }

    private async Task<Result<string>> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
    {
        var cachedToken = await cache.GetStringAsync(tokenCacheKey, cancellationToken);
        if (cachedToken.HasValue()) return Result<string>.Success(cachedToken!);

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
                logger.LogError("Failed to obtain service account token. Status: {StatusCode}", response.StatusCode);
                return Result<string>.Failure(RegistrationErrors.IdentityProviderUnavailable);
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(jsonOptions, cancellationToken);

            await cache.SetStringAsync(
                tokenCacheKey,
                tokenResponse!.AccessToken,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.ExpiresIn) - tokenExpiryBuffer,
                },
                cancellationToken);

            return Result<string>.Success(tokenResponse.AccessToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while obtaining service account token");
            return Result<string>.Failure(RegistrationErrors.IdentityProviderUnavailable);
        }
    }

    private async Task<Result<KeycloakUserId>> CreateKeycloakUserAsync(
        Email email,
        Password password,
        DisplayName displayName,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var url = $"/admin/realms/{options.Value.Realm}/users";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(BuildUserPayload(email, password, displayName), options: jsonOptions);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            return await HandleUserCreationResponseAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while creating Keycloak user");
            return Result<KeycloakUserId>.Failure(RegistrationErrors.IdentityProviderUnavailable);
        }
    }

    private async Task<Result<KeycloakUserId>> HandleUserCreationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return Result<KeycloakUserId>.Failure(RegistrationErrors.EmailAlreadyExists);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create Keycloak user. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
            return Result<KeycloakUserId>.Failure(RegistrationErrors.UserCreationFailed);
        }

        return ExtractKeycloakUserId(response);
    }

    private Result<KeycloakUserId> ExtractKeycloakUserId(HttpResponseMessage response)
    {
        var locationHeader = response.Headers.Location?.ToString();
        if (!locationHeader.HasValue())
        {
            logger.LogError("Keycloak did not return a Location header after user creation");
            return Result<KeycloakUserId>.Failure(RegistrationErrors.UserCreationFailed);
        }

        var keycloakUserIdString = locationHeader!.Split('/').LastOrDefault();
        if (!keycloakUserIdString.HasValue())
        {
            logger.LogError("Malformed Location header: {LocationHeader}", locationHeader);
            return Result<KeycloakUserId>.Failure(RegistrationErrors.UserCreationFailed);
        }

        return Result<KeycloakUserId>.Success(KeycloakUserId.From(keycloakUserIdString!));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record KeycloakUserRepresentation(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("email")] string Email);
}
