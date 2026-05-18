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
using SmartSolutionsLab.Yumney.Shared.Outcomes;
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
	private static readonly TimeSpan minTokenCacheLifetime = TimeSpan.FromSeconds(5);
	private static readonly SemaphoreSlim tokenRefreshGate = new(1, 1);
	private static readonly string[] verifyEmailActions = ["VERIFY_EMAIL"];

	public async Task<Result<KeycloakUserId>> CreateUserAsync(
		Email email,
		Password password,
		DisplayName displayName,
		CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.create_user");
		activity?.SetTag("keycloak.email", email.Value);

		var url = $"/admin/realms/{options.Value.Realm}/users";
		var content = JsonContent.Create(BuildUserPayload(email, password, displayName), options: jsonOptions);

		var response = await SendAuthenticatedAsync(
			HttpMethod.Post, url, content, "create_user", RegistrationErrors.IdentityProviderUnavailable, cancellationToken);
		if (response.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, response.Error!.Message);
			return response.Error!;
		}

		var result = await HandleUserCreationResponseAsync(response.Value, cancellationToken);
		activity?.SetStatus(result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Error?.Message);
		return result;
	}

	public async Task<Result<KeycloakUserId>> FindUserByEmailAsync(Email email, CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.find_user");
		activity?.SetTag("keycloak.email", email.Value);

		var encodedEmail = Uri.EscapeDataString(email.Value);
		var url = $"/admin/realms/{options.Value.Realm}/users?email={encodedEmail}&exact=true";

		var response = await SendAuthenticatedAsync(
			HttpMethod.Get, url, null, "search_users", VerificationErrors.IdentityProviderUnavailable, cancellationToken);
		if (response.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, response.Error!.Message);
			return response.Error!;
		}

		if (!response.Value.IsSuccessStatusCode)
		{
			LogSearchUsersFailed(response.Value.StatusCode);
			activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.Value.StatusCode}");
			return VerificationErrors.IdentityProviderUnavailable;
		}

		var users = await response.Value.Content.ReadFromJsonAsync<KeycloakUserRepresentation[]>(jsonOptions, cancellationToken);
		if (users is null || users.Length == 0)
		{
			activity?.SetStatus(ActivityStatusCode.Error, "User not found");
			return VerificationErrors.UserNotFound;
		}

		activity?.SetStatus(ActivityStatusCode.Ok);
		return KeycloakUserId.From(users[0].Id);
	}

	public async Task<Result> SendVerificationEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.send_verification_email");

		var url = $"/admin/realms/{options.Value.Realm}/users/{keycloakUserId.Value}/execute-actions-email";
		var content = JsonContent.Create(verifyEmailActions, options: jsonOptions);

		var response = await SendAuthenticatedAsync(
			HttpMethod.Put, url, content, "send_verification", VerificationErrors.IdentityProviderUnavailable, cancellationToken);
		if (response.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, response.Error!.Message);
			return Result.Failure(response.Error!);
		}

		if (!response.Value.IsSuccessStatusCode)
		{
			var body = await response.Value.Content.ReadAsStringAsync(cancellationToken);
			activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.Value.StatusCode}");
			LogSendVerificationFailed(response.Value.StatusCode, body);
			return Result.Failure(VerificationErrors.SendFailed);
		}

		activity?.SetStatus(ActivityStatusCode.Ok);
		return Result.Success();
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
			requiredActions = verifyEmailActions,
			credentials = new[]
			{
				new { type = "password", value = password.Value, temporary = false },
			},
		};
	}

	private async Task<Result<HttpResponseMessage>> SendAuthenticatedAsync(
		HttpMethod method,
		string url,
		HttpContent? content,
		string operation,
		ApiError transportFailureError,
		CancellationToken cancellationToken)
	{
		var token = await GetServiceAccountTokenAsync(cancellationToken);
		if (token.IsFailure) return transportFailureError;

		using var request = new HttpRequestMessage(method, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
		if (content is not null) request.Content = content;

		try
		{
			return await httpClient.SendAsync(request, cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			LogTransportError(ex, operation);
			return transportFailureError;
		}
	}

	private async Task<Result<string>> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
	{
		var cachedToken = await cache.GetStringAsync(tokenCacheKey, cancellationToken);
		if (cachedToken.HasValue()) return cachedToken!;

		await tokenRefreshGate.WaitAsync(cancellationToken);
		try
		{
			cachedToken = await cache.GetStringAsync(tokenCacheKey, cancellationToken);
			if (cachedToken.HasValue()) return cachedToken!;

			return await FetchAndCacheTokenAsync(cancellationToken);
		}
		finally
		{
			tokenRefreshGate.Release();
		}
	}

	private async Task<Result<string>> FetchAndCacheTokenAsync(CancellationToken cancellationToken)
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
				LogTokenAcquisitionFailed(response.StatusCode);
				return RegistrationErrors.IdentityProviderUnavailable;
			}

			var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(jsonOptions, cancellationToken);
			if (tokenResponse is null || !tokenResponse.AccessToken.HasValue())
			{
				LogTokenResponseMalformed();
				return RegistrationErrors.IdentityProviderUnavailable;
			}

			var lifetime = TimeSpan.FromSeconds(tokenResponse.ExpiresIn) - tokenExpiryBuffer;
			if (lifetime < minTokenCacheLifetime) lifetime = minTokenCacheLifetime;

			await cache.SetStringAsync(
				tokenCacheKey,
				tokenResponse.AccessToken,
				new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
				cancellationToken);

			return tokenResponse.AccessToken;
		}
		catch (HttpRequestException ex)
		{
			LogTransportError(ex, "token_acquisition");
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

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to send verification email. Status: {StatusCode}, Body: {Body}")]
	private partial void LogSendVerificationFailed(HttpStatusCode statusCode, string body);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to obtain service account token. Status: {StatusCode}")]
	private partial void LogTokenAcquisitionFailed(HttpStatusCode statusCode);

	[LoggerMessage(Level = LogLevel.Error, Message = "Keycloak token endpoint returned a malformed or empty body")]
	private partial void LogTokenResponseMalformed();

	[LoggerMessage(Level = LogLevel.Error, Message = "Keycloak HTTP transport error during {Operation}")]
	private partial void LogTransportError(Exception ex, string operation);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to create Keycloak user. Status: {StatusCode}, Body: {Body}")]
	private partial void LogCreateUserFailed(HttpStatusCode statusCode, string body);

	[LoggerMessage(Level = LogLevel.Error, Message = "Keycloak did not return a Location header after user creation")]
	private partial void LogMissingLocationHeader();

	[LoggerMessage(Level = LogLevel.Error, Message = "Malformed Location header: {LocationHeader}")]
	private partial void LogMalformedLocationHeader(string locationHeader);
}
