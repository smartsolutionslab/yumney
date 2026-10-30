using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.AI;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// <see cref="IExtractionResultCache"/> on the host's <see cref="IDistributedCache"/>
/// (Redis in every deployed host), so all recipes-api replicas share one cache
/// instead of each paying for its own LLM call. The cache is an optimisation
/// only: any cache error is logged and treated as a miss.
/// </summary>
#pragma warning disable SA1303, SA1311
public sealed partial class DistributedExtractionResultCache(
	IDistributedCache cache,
	ILogger<DistributedExtractionResultCache> logger)
	: IExtractionResultCache
{
	// Bump the version when ExtractedRecipeDto changes shape so stale entries are ignored.
	private const string keyPrefix = "recipes:extraction:v1:";

	private static readonly DistributedCacheEntryOptions entryOptions = new()
	{
		AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
	};
#pragma warning restore SA1303, SA1311

	public string ComputeKey(string cleanedText)
	{
		if (string.IsNullOrEmpty(cleanedText)) return "empty";

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(cleanedText));
		return Convert.ToHexStringLower(hash.AsSpan(0, 16));
	}

	public async Task<ExtractedRecipeDto?> GetAsync(string key, CancellationToken cancellationToken = default)
	{
		try
		{
			var cached = await cache.GetAsync(keyPrefix + key, cancellationToken);
			return cached is null ? null : JsonSerializer.Deserialize<ExtractedRecipeDto>(cached, LlmJson.SerializerOptions);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogCacheReadFailed(key, ex.Message);
			return null;
		}
	}

	public async Task SetAsync(string key, ExtractedRecipeDto recipe, CancellationToken cancellationToken = default)
	{
		try
		{
			var payload = JsonSerializer.SerializeToUtf8Bytes(recipe, LlmJson.SerializerOptions);
			await cache.SetAsync(keyPrefix + key, payload, entryOptions, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogCacheWriteFailed(key, ex.Message);
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Extraction cache read failed for key {CacheKey}; treating as a miss: {Reason}")]
	private partial void LogCacheReadFailed(string cacheKey, string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Extraction cache write failed for key {CacheKey}: {Reason}")]
	private partial void LogCacheWriteFailed(string cacheKey, string reason);
}
