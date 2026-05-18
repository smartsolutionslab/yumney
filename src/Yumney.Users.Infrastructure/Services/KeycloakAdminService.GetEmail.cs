using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1601
public sealed partial class KeycloakAdminService
{
	public async Task<Result<Email>> GetEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.get_email");
		activity?.SetTag("keycloak.user_id", keycloakUserId.Value);

		var url = $"/admin/realms/{options.Value.Realm}/users/{keycloakUserId.Value}";

		var response = await SendAuthenticatedAsync(
			HttpMethod.Get, url, null, "get_email", VerificationErrors.IdentityProviderUnavailable, cancellationToken);
		if (response.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, response.Error!.Message);
			return Result<Email>.Failure(response.Error!);
		}

		if (response.Value.StatusCode == HttpStatusCode.NotFound)
		{
			activity?.SetStatus(ActivityStatusCode.Error, "user not found");
			LogGetEmailUserMissing(keycloakUserId.Value);
			return Result<Email>.Failure(VerificationErrors.IdentityProviderUnavailable);
		}

		if (!response.Value.IsSuccessStatusCode)
		{
			var body = await response.Value.Content.ReadAsStringAsync(cancellationToken);
			activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.Value.StatusCode}");
			LogGetEmailFailed(response.Value.StatusCode, body);
			return Result<Email>.Failure(VerificationErrors.IdentityProviderUnavailable);
		}

		var representation = await response.Value.Content.ReadFromJsonAsync<KeycloakUserEmailRepresentation>(jsonOptions, cancellationToken);
		if (representation?.Email is null)
		{
			activity?.SetStatus(ActivityStatusCode.Error, "email missing on Keycloak user");
			LogGetEmailMissing(keycloakUserId.Value);
			return Result<Email>.Failure(VerificationErrors.IdentityProviderUnavailable);
		}

		activity?.SetStatus(ActivityStatusCode.Ok);
		return Result<Email>.Success(Email.From(representation.Email));
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Keycloak user {KeycloakUserId} not found while reading email")]
	private partial void LogGetEmailUserMissing(string keycloakUserId);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Keycloak user {KeycloakUserId} has no email field set")]
	private partial void LogGetEmailMissing(string keycloakUserId);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to read Keycloak user email. Status: {StatusCode}, Body: {Body}")]
	private partial void LogGetEmailFailed(HttpStatusCode statusCode, string body);

#pragma warning disable SA1402, SA1649
	private sealed record KeycloakUserEmailRepresentation(
		[property: JsonPropertyName("email")] string? Email);
#pragma warning restore SA1402, SA1649
}
