using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
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

		var url = $"/admin/realms/{options.Value.Realm}/users/{keycloakUserId.Value}";

		var response = await SendAuthenticatedAsync(HttpMethod.Delete, url, null, "delete_user", VerificationErrors.IdentityProviderUnavailable, cancellationToken);
		if (response.IsFailure)
		{
			activity?.SetStatus(ActivityStatusCode.Error, response.Error!.Message);
			return Result.Failure(response.Error!);
		}

		// 404 means the user is already gone — treat as success so the call is idempotent.
		if (response.Value.IsSuccessStatusCode || response.Value.StatusCode == HttpStatusCode.NotFound)
		{
			activity?.SetStatus(ActivityStatusCode.Ok);
			return Result.Success();
		}

		var body = await response.Value.Content.ReadAsStringAsync(cancellationToken);
		activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.Value.StatusCode}");
		LogDeleteUserFailed(response.Value.StatusCode, body);
		return Result.Failure(VerificationErrors.IdentityProviderUnavailable);
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete Keycloak user. Status: {StatusCode}, Body: {Body}")]
	private partial void LogDeleteUserFailed(HttpStatusCode statusCode, string body);
}
