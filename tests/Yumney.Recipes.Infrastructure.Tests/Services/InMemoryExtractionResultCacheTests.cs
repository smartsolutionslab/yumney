using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class InMemoryExtractionResultCacheTests
{
	[Fact]
	public void ComputeKey_SameText_SameKey()
	{
		var cache = new InMemoryExtractionResultCache();

		var a = cache.ComputeKey("Pancakes with syrup");
		var b = cache.ComputeKey("Pancakes with syrup");

		a.Should().Be(b);
	}

	[Fact]
	public void ComputeKey_DifferentText_DifferentKey()
	{
		var cache = new InMemoryExtractionResultCache();

		var a = cache.ComputeKey("Pancakes");
		var b = cache.ComputeKey("Pancakes!");

		a.Should().NotBe(b);
	}

	[Fact]
	public async Task Get_Unknown_ReturnsNull()
	{
		var cache = new InMemoryExtractionResultCache();

		var result = await cache.GetAsync("no-such-key");

		result.Should().BeNull();
	}

	[Fact]
	public async Task SetThenGet_RoundTripsRecipe()
	{
		var cache = new InMemoryExtractionResultCache();
		var key = cache.ComputeKey("hello");
		var recipe = new ExtractedRecipeDto(
			Title: "Hello Recipe",
			Ingredients: [new ExtractedIngredientDto("water", 1, "cup")],
			Steps: [new ExtractedStepDto(1, "boil")]);

		await cache.SetAsync(key, recipe);
		var loaded = await cache.GetAsync(key);

		loaded.Should().Be(recipe);
	}

	[Fact]
	public async Task Get_ExpiredEntry_ReturnsNull()
	{
		// TTL of zero means the entry expires immediately after being stored.
		var cache = new InMemoryExtractionResultCache(TimeSpan.Zero, maxEntries: 10);
		var key = cache.ComputeKey("x");
		await cache.SetAsync(key, StubRecipe());

		var loaded = await cache.GetAsync(key);

		loaded.Should().BeNull();
	}

	[Fact]
	public async Task Set_BeyondCapacity_EvictsOldestEntry()
	{
		var cache = new InMemoryExtractionResultCache(TimeSpan.FromHours(1), maxEntries: 2);
		await cache.SetAsync("a", StubRecipe("a"));
		await Task.Delay(10); // stagger expiresAt so "a" is oldest
		await cache.SetAsync("b", StubRecipe("b"));
		await Task.Delay(10);
		await cache.SetAsync("c", StubRecipe("c"));

		var a = await cache.GetAsync("a");
		var b = await cache.GetAsync("b");
		var c = await cache.GetAsync("c");

		a.Should().BeNull("the oldest entry is evicted when the cache is full");
		b.Should().NotBeNull();
		c.Should().NotBeNull();
	}

	private static ExtractedRecipeDto StubRecipe(string name = "R") => new(
		Title: name,
		Ingredients: [new ExtractedIngredientDto("x", null, null)],
		Steps: [new ExtractedStepDto(1, "x")]);
}
