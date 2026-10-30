using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class DistributedExtractionResultCacheTests
{
	private static readonly ExtractedRecipeDto Recipe = new(
		"Soup",
		[new ExtractedIngredientDto("Water", 1.5m, "l")],
		[new ExtractedStepDto(1, "Boil")],
		Servings: 2);

	[Fact]
	public void ComputeKey_SameText_SameKey()
	{
		var cache = CreateCache();

		cache.ComputeKey("hello").Should().Be(cache.ComputeKey("hello"));
	}

	[Fact]
	public void ComputeKey_DifferentText_DifferentKey()
	{
		var cache = CreateCache();

		cache.ComputeKey("hello").Should().NotBe(cache.ComputeKey("world"));
	}

	[Fact]
	public async Task GetAsync_UnknownKey_ReturnsNull()
	{
		var result = await CreateCache().GetAsync("missing");

		result.Should().BeNull();
	}

	[Fact]
	public async Task SetThenGet_RoundTripsRecipe()
	{
		var cache = CreateCache();

		await cache.SetAsync("key", Recipe);
		var result = await cache.GetAsync("key");

		result.Should().BeEquivalentTo(Recipe);
	}

	[Fact]
	public async Task SetAsync_StoresUnderPrefixedKeyWith24HourExpiry()
	{
		var distributedCache = Substitute.For<IDistributedCache>();

		await CreateCache(distributedCache).SetAsync("abc", Recipe);

		await distributedCache.Received(1).SetAsync(
			"recipes:extraction:v1:abc",
			Arg.Any<byte[]>(),
			Arg.Is<DistributedCacheEntryOptions>(options => options.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(24)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetAsync_CacheUnavailable_ReturnsNull()
	{
		var distributedCache = Substitute.For<IDistributedCache>();
		distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException("redis down"));

		var result = await CreateCache(distributedCache).GetAsync("key");

		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAsync_CacheUnavailable_DoesNotThrow()
	{
		var distributedCache = Substitute.For<IDistributedCache>();
		distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("redis down"));

		var act = () => CreateCache(distributedCache).SetAsync("key", Recipe);

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task GetAsync_CorruptEntry_ReturnsNull()
	{
		var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		await distributedCache.SetAsync("recipes:extraction:v1:key", "not json"u8.ToArray());

		var result = await CreateCache(distributedCache).GetAsync("key");

		result.Should().BeNull();
	}

	internal static DistributedExtractionResultCache CreateCache(IDistributedCache? distributedCache = null) =>
		new(
			distributedCache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
			NullLogger<DistributedExtractionResultCache>.Instance);
}
