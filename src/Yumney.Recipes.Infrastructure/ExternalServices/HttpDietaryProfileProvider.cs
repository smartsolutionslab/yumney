using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

#pragma warning disable SA1601
public sealed partial class HttpDietaryProfileProvider(
	IHttpClientFactory httpClientFactory,
	ILogger<HttpDietaryProfileProvider> logger) : IDietaryProfileProvider
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
#pragma warning restore SA1311

	public async Task<DietaryProfileSnapshot> GetAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var client = httpClientFactory.CreateClient("users-api");
			var response = await client.GetAsync("/api/v1/users/me/profile", cancellationToken);

			if (response.StatusCode == HttpStatusCode.NotFound) return DietaryProfileSnapshot.Empty;
			if (!response.IsSuccessStatusCode)
			{
				LogProfileFetchFailed($"HTTP {(int)response.StatusCode}");
				return DietaryProfileSnapshot.Empty;
			}

			var dto = await response.Content.ReadFromJsonAsync<UserProfileResponse>(jsonOptions, cancellationToken);
			var dietary = dto?.DietaryProfile;
			if (dietary is null) return DietaryProfileSnapshot.Empty;

			return new DietaryProfileSnapshot(
				dietary.DietaryType,
				dietary.Restrictions ?? []);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogProfileFetchFailed(ex.Message);
			return DietaryProfileSnapshot.Empty;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch dietary profile from users-api ({Reason}); recipe-suggestion handler continuing without dietary preferences.")]
	private partial void LogProfileFetchFailed(string reason);

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record UserProfileResponse(DietaryProfilePayload? DietaryProfile);

	private sealed record DietaryProfilePayload(string? DietaryType, IReadOnlyList<string>? Restrictions);
}
