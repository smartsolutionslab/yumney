using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;

#pragma warning disable SA1601
public sealed partial class HttpStaplesProvider(IHttpClientFactory httpClientFactory, ILogger<HttpStaplesProvider> logger)
	: IStaplesProvider
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
#pragma warning restore SA1311

	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var client = httpClientFactory.CreateClient("users-api");
			var url = "/api/v1/users/staples";
			var staples = await client.GetFromJsonAsync<List<string>>(url, jsonOptions, cancellationToken) ?? [];

			return staples.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogStaplesFetchFailed(ex.Message);
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch staples from users-api ({Reason}); ingredient-balance continuing without staple augmentation.")]
	private partial void LogStaplesFetchFailed(string reason);
}
