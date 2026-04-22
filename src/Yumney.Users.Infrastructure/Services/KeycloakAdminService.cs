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

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1303 // Const field names should begin with upper-case letter (editorconfig requires camelCase for private fields)
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase for private fields)
#pragma warning disable SA1601
public sealed partial class KeycloakAdminService(
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

	public async Task<Result<KeycloakUserId>> CreateUserAsync(Email email, Password password, DisplayName displayName, CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.create_user");
		activity?.SetTag("keycloak.email", email.Value);

		var token = await GetServiceAccountTokenAsync(cancellationToken);
		if (token.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, "Token acquisition failed");
			return token.Error!;
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
			return VerificationErrors.IdentityProviderUnavailable;
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
				LogSearchUsersFailed(response.StatusCode);
				return VerificationErrors.IdentityProviderUnavailable;
			}

			var users = await response.Content.ReadFromJsonAsync<KeycloakUserRepresentation[]>(jsonOptions, cancellationToken);

			if (users is null || users.Length == 0)
			{
				activity?.SetStatus(ActivityStatusCode.Error, "User not found");
				return VerificationErrors.UserNotFound;
			}

			activity?.SetStatus(ActivityStatusCode.Ok);
			return KeycloakUserId.From(users[0].Id);
		}
		catch (HttpRequestException ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogSearchUsersHttpError(ex);
			return VerificationErrors.IdentityProviderUnavailable;
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
				LogSendVerificationFailed(response.StatusCode, body);
				return Result.Failure(VerificationErrors.SendFailed);
			}

			activity?.SetStatus(ActivityStatusCode.Ok);
			return Result.Success();
		}
		catch (HttpRequestException ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogSendVerificationHttpError(ex);
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
				tokenResponse!.AccessToken,
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
			LogCreateUserHttpError(ex);
			return RegistrationErrors.IdentityProviderUnavailable;
		}
	}

	private async Task<Result<KeycloakUserId>> HandleUserCreationResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.StatusCode == HttpStatusCode.Conflict) return RegistrationErrors.EmailAlreadyExists;

		if (!response.IsSuccessStatusCode)
		{
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			LogCreateUserFailed(response.StatusCode, body);
			return RegistrationErrors.UserCreationFailed;
		}

		return ExtractKeycloakUserId(response);
	}

	private Result<KeycloakUserId> ExtractKeycloakUserId(HttpResponseMessage response)
	{
		var locationHeader = response.Headers.Location?.ToString();
		if (!locationHeader.HasValue())
		{
			LogMissingLocationHeader();
			return RegistrationErrors.UserCreationFailed;
		}

		var keycloakUserIdString = locationHeader!.Split('/').LastOrDefault();
		if (!keycloakUserIdString.HasValue())
		{
			LogMalformedLocationHeader(locationHeader);
			return RegistrationErrors.UserCreationFailed;
		}

		return KeycloakUserId.From(keycloakUserIdString!);
	}

	private sealed record TokenResponse(
		[property: JsonPropertyName("access_token")] string AccessToken,
		[property: JsonPropertyName("expires_in")] int ExpiresIn);

	private sealed record KeycloakUserRepresentation(
		[property: JsonPropertyName("id")] string Id,
		[property: JsonPropertyName("email")] string Email);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to search Keycloak users. Status: {StatusCode}")]
	private partial void LogSearchUsersFailed(HttpStatusCode statusCode);

	[LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while searching Keycloak users")]
	private partial void LogSearchUsersHttpError(Exception ex);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to send verification email. Status: {StatusCode}, Body: {Body}")]
	private partial void LogSendVerificationFailed(HttpStatusCode statusCode, string body);

	[LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while sending verification email")]
	private partial void LogSendVerificationHttpError(Exception ex);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to obtain service account token. Status: {StatusCode}")]
	private partial void LogTokenAcquisitionFailed(HttpStatusCode statusCode);

	[LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while obtaining service account token")]
	private partial void LogTokenAcquisitionHttpError(Exception ex);

	[LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while creating Keycloak user")]
	private partial void LogCreateUserHttpError(Exception ex);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to create Keycloak user. Status: {StatusCode}, Body: {Body}")]
	private partial void LogCreateUserFailed(HttpStatusCode statusCode, string body);

	[LoggerMessage(Level = LogLevel.Error, Message = "Keycloak did not return a Location header after user creation")]
	private partial void LogMissingLocationHeader();

	[LoggerMessage(Level = LogLevel.Error, Message = "Malformed Location header: {LocationHeader}")]
	private partial void LogMalformedLocationHeader(string locationHeader);
}
