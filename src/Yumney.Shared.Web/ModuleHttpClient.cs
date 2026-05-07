using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Web;

#pragma warning disable SA1601
internal sealed partial class ModuleHttpClient(
	HttpClient httpClient,
	string upstreamName,
	ILogger<ModuleHttpClient> logger) : IModuleHttpClient
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};
#pragma warning restore SA1311

	public async Task<TResult> GetOrDefaultAsync<TResult>(
		string url,
		TResult fallback,
		string operation,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await httpClient.GetFromJsonAsync<TResult>(url, jsonOptions, cancellationToken);
			return result ?? fallback;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogGetFailed(operation, upstreamName, ex.Message);
			return fallback;
		}
	}

	public async Task<TResult?> FindAsync<TResult>(
		string url,
		string operation,
		CancellationToken cancellationToken = default)
		where TResult : class
	{
		try
		{
			using var response = await httpClient.GetAsync(url, cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound) return null;
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadFromJsonAsync<TResult>(jsonOptions, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogFindFailed(operation, upstreamName, ex.Message);
			return null;
		}
	}

	public async Task PostAsync<TBody>(
		string url,
		TBody body,
		string operation,
		CancellationToken cancellationToken = default)
	{
		try
		{
			using var response = await httpClient.PostAsJsonAsync(url, body, jsonOptions, cancellationToken);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogPostFailed(operation, upstreamName, ex.Message);
			throw;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "{Operation} GET against {Upstream} failed ({Reason}); returning fallback.")]
	private partial void LogGetFailed(string operation, string upstream, string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "{Operation} GET against {Upstream} failed ({Reason}); returning null.")]
	private partial void LogFindFailed(string operation, string upstream, string reason);

	[LoggerMessage(Level = LogLevel.Error, Message = "{Operation} POST to {Upstream} failed ({Reason}).")]
	private partial void LogPostFailed(string operation, string upstream, string reason);
}
