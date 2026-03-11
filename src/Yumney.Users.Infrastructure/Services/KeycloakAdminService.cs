using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase for private fields)
public sealed class KeycloakAdminService(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakAdminService> logger)
    : IKeycloakAdminService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] verifyEmailActions = ["VERIFY_EMAIL"];

    public async Task<Result<KeycloakUserId>> CreateUserAsync(
        Email email,
        Password password,
        DisplayName displayName,
        CancellationToken cancellationToken = default)
    {
        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
            return Result<KeycloakUserId>.Failure(token.Error!);
        }

        return await CreateKeycloakUserAsync(email, password, displayName, token.Value, cancellationToken);
    }

    public async Task<Result<KeycloakUserId>> FindUserByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
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
                return Result<KeycloakUserId>.Failure(VerificationErrors.UserNotFound);
            }

            return Result<KeycloakUserId>.Success(new KeycloakUserId(users[0].Id));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while searching Keycloak users");
            return Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable);
        }
    }

    public async Task<Result> SendVerificationEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
    {
        var token = await GetServiceAccountTokenAsync(cancellationToken);
        if (token.IsFailure)
        {
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
                logger.LogError("Failed to send verification email. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return Result.Failure(VerificationErrors.SendFailed);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while sending verification email");
            return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
        }
    }

    private async Task<Result<string>> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
    {
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

            return Result<string>.Success(tokenResponse!.AccessToken);
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
        var userPayload = new
        {
            username = email.Value,
            email = email.Value,
            enabled = true,
            emailVerified = false,
            firstName = displayName.Value,
            requiredActions = new[] { "VERIFY_EMAIL" },
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = password.Value,
                    temporary = false,
                },
            },
        };

        var url = $"/admin/realms/{options.Value.Realm}/users";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(userPayload, options: jsonOptions);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

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

            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
            {
                logger.LogError("Keycloak did not return a Location header after user creation");
                return Result<KeycloakUserId>.Failure(RegistrationErrors.UserCreationFailed);
            }

            var keycloakUserIdString = locationHeader.Split('/').Last();
            return Result<KeycloakUserId>.Success(new KeycloakUserId(keycloakUserIdString));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while creating Keycloak user");
            return Result<KeycloakUserId>.Failure(RegistrationErrors.IdentityProviderUnavailable);
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record KeycloakUserRepresentation(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("email")] string Email);
}
