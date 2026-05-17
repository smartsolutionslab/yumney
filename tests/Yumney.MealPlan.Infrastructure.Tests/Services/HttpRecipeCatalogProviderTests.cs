using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpRecipeCatalogProviderTests
{
	private readonly IRecipesClient recipes = Substitute.For<IRecipesClient>();

	[Fact]
	public async Task ListAsync_ForwardsPageSizeToClient()
	{
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse([]));

		await new HttpRecipeCatalogProvider(recipes).ListAsync(pageSize: 25);

		await recipes.Received(1).ListRecipeCatalogAsync(25, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ListAsync_EmptyResponse_ReturnsEmpty()
	{
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse([]));

		var entries = await new HttpRecipeCatalogProvider(recipes).ListAsync(pageSize: 10);

		entries.Should().BeEmpty();
	}

	[Fact]
	public async Task ListAsync_MapsEveryFieldFromCatalogItemToEntry()
	{
		var identifier = Guid.NewGuid();
		recipes.ListRecipeCatalogAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new RecipeCatalogResponse(
			[
				new RecipeCatalogItem(
					Identifier: identifier,
					Title: "Carbonara",
					PrepTimeMinutes: 10,
					CookTimeMinutes: 15,
					Difficulty: "easy",
					Tags: ["italian", "pasta"],
					IsFavorite: true,
					Rating: 5),
			]));

		var entries = await new HttpRecipeCatalogProvider(recipes).ListAsync(pageSize: 10);

		entries.Should().ContainSingle();
		var entry = entries[0];
		entry.RecipeIdentifier.Should().Be(identifier);
		entry.Title.Should().Be("Carbonara");
		entry.PrepTimeMinutes.Should().Be(10);
		entry.CookTimeMinutes.Should().Be(15);
		entry.Difficulty.Should().Be("easy");
		entry.Tags.Should().Equal("italian", "pasta");
		entry.IsFavorite.Should().BeTrue();
		entry.Rating.Should().Be(5);
	}
}
