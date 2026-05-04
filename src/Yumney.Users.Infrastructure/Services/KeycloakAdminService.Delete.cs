using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1601
public sealed partial class KeycloakAdminService
{
	public async Task<Result> DeleteUserAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		using var activity = UsersDiagnostics.ActivitySource.StartActivity("keycloak.delete_user");
		activity?.SetTag("keycloak.user_id", keycloakUserId.Value);

		var token = await GetServiceAccountTokenAsync(cancellationToken);
		if (token.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, "Token acquisition failed");
			return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
		}

		var url = $"/admin/realms/{options.Value.Realm}/users/{keycloakUserId.Value}";
		using var request = new HttpRequestMessage(HttpMethod.Delete, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

		try
		{
			var response = await httpClient.SendAsync(request, cancellationToken);

			// 404 means the user is already gone — treat as success so the call is idempotent.
			if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
			{
				activity?.SetStatus(ActivityStatusCode.Ok);
				return Result.Success();
			}

			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
			LogDeleteUserFailed(response.StatusCode, body);
			return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
		}
		catch (HttpRequestException ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogDeleteUserHttpError(ex);
			return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
		}
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete Keycloak user. Status: {StatusCode}, Body: {Body}")]
	private partial void LogDeleteUserFailed(HttpStatusCode statusCode, string body);

	[LoggerMessage(Level = LogLevel.Error, Message = "HTTP error while deleting Keycloak user")]
	private partial void LogDeleteUserHttpError(Exception ex);
}
