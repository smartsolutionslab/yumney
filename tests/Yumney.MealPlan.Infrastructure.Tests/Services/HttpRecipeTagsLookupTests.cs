using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpRecipeTagsLookupTests
{
	private readonly IRecipesClient recipes = Substitute.For<IRecipesClient>();

	[Fact]
	public async Task GetAllAsync_UsesPageSize100ForCatalogScan()
	{
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse([]));

		await new HttpRecipeTagsLookup(recipes).GetAllAsync();

		await recipes.Received(1).ListRecipeCatalogAsync(100, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetAllAsync_BuildsDictionaryKeyedByRecipeIdentifier()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse(
			[
				new RecipeCatalogItem(first, "A", null, null, null, ["italian"], false, null),
				new RecipeCatalogItem(second, "B", null, null, null, ["asian", "spicy"], false, null),
			]));

		var map = await new HttpRecipeTagsLookup(recipes).GetAllAsync();

		map.Should().HaveCount(2);
		map[first].Should().Equal("italian");
		map[second].Should().Equal("asian", "spicy");
	}

	[Fact]
	public async Task GetAllAsync_EmptyResponse_ReturnsEmptyMap()
	{
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse([]));

		var map = await new HttpRecipeTagsLookup(recipes).GetAllAsync();

		map.Should().BeEmpty();
	}
}
