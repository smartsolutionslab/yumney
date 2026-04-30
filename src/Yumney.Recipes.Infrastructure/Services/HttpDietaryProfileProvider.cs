using System.Net;
using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

/// <summary>
/// HTTP-backed <see cref="IDietaryProfileProvider"/> for callers in the
/// Recipes module. Reads the profile via Users API. Returns
/// <see cref="DietaryProfileSnapshot.Empty"/> if the user has not set
/// preferences yet (404) — recipe suggestions then run unfiltered.
/// </summary>
public sealed class HttpDietaryProfileProvider(IHttpClientFactory httpClientFactory) : IDietaryProfileProvider
{
	public async Task<DietaryProfileSnapshot> GetAsync(string ownerId, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("users-api");
		var response = await client.GetAsync("/api/v1/users/me/profile", cancellationToken);

		if (response.StatusCode == HttpStatusCode.NotFound) return DietaryProfileSnapshot.Empty;
		response.EnsureSuccessStatusCode();

		var dto = await response.Content.ReadFromJsonAsync<UserProfileResponse>(cancellationToken: cancellationToken);
		var dietary = dto?.DietaryProfile;
		if (dietary is null) return DietaryProfileSnapshot.Empty;

		return new DietaryProfileSnapshot(
			dietary.DietaryType,
			dietary.Restrictions ?? []);
	}

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record UserProfileResponse(DietaryProfilePayload? DietaryProfile);

	private sealed record DietaryProfilePayload(string? DietaryType, IReadOnlyList<string>? Restrictions);
}
