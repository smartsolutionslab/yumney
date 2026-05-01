using System.Net;
using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpDietaryProfileProvider(IHttpClientFactory httpClientFactory) : IDietaryProfileProvider
{
	public async Task<DietaryProfileSnapshot> GetAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
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
