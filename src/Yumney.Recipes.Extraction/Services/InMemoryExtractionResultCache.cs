using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// Default <see cref="IExtractionResultCache"/> — bounded concurrent
/// dictionary with a fixed TTL. Good enough for a single-process API;
/// swap for a Redis-backed implementation when we horizontally scale.
/// </summary>
#pragma warning disable SA1311
public sealed class InMemoryExtractionResultCache : IExtractionResultCache
{
	private static readonly TimeSpan defaultTtl = TimeSpan.FromHours(24);
#pragma warning restore SA1311

	private readonly ConcurrentDictionary<string, Entry> entries = new();
	private readonly TimeSpan ttl;
	private readonly int maxEntries;

	public InMemoryExtractionResultCache()
		: this(defaultTtl, maxEntries: 1_000)
	{
	}

	public InMemoryExtractionResultCache(TimeSpan ttl, int maxEntries)
	{
		this.ttl = ttl;
		this.maxEntries = maxEntries;
	}

	public string ComputeKey(string cleanedText)
	{
		if (string.IsNullOrEmpty(cleanedText)) return "empty";

		Span<byte> buffer = stackalloc byte[32];
		SHA256.HashData(Encoding.UTF8.GetBytes(cleanedText), buffer);

		var sb = new StringBuilder(32);
		for (var i = 0; i < 16; i++)
		{
			sb.Append(buffer[i].ToString("x2", CultureInfo.InvariantCulture));
		}

		return sb.ToString();
	}

	public Task<ExtractedRecipeDto?> GetAsync(string key, CancellationToken cancellationToken = default)
	{
		if (!entries.TryGetValue(key, out var entry)) return Task.FromResult<ExtractedRecipeDto?>(null);
		if (entry.ExpiresAt <= DateTime.UtcNow)
		{
			entries.TryRemove(key, out _);
			return Task.FromResult<ExtractedRecipeDto?>(null);
		}

		return Task.FromResult<ExtractedRecipeDto?>(entry.Recipe);
	}

	public Task SetAsync(string key, ExtractedRecipeDto recipe, CancellationToken cancellationToken = default)
	{
		if (entries.Count >= maxEntries) EvictOldest();

		entries[key] = new Entry(recipe, DateTime.UtcNow.Add(ttl));
		return Task.CompletedTask;
	}

	private void EvictOldest()
	{
		// Drop the entry with the earliest ExpiresAt. Fast enough at
		// 1000 entries; if that ever gets hot we'll go LRU with a
		// doubly-linked list. Not today.
		var oldest = default(KeyValuePair<string, Entry>);
		var oldestExpiry = DateTime.MaxValue;
		foreach (var kvp in entries)
		{
			if (kvp.Value.ExpiresAt < oldestExpiry)
			{
				oldest = kvp;
				oldestExpiry = kvp.Value.ExpiresAt;
			}
		}

		if (oldest.Key is not null) entries.TryRemove(oldest.Key, out _);
	}

	private sealed record Entry(ExtractedRecipeDto Recipe, DateTime ExpiresAt);
}
