using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Queries;

#pragma warning disable SA1601
public partial class GetRecipesQueryHandlerTests
#pragma warning restore SA1601
{
	[Fact]
	public async Task HandleAsync_RecipeWithAllFields_MapsOptionalFieldsCorrectly()
	{
		var recipe = RecipeTestData.CreateRecipeWithOptionals();
		SetupRepository([recipe], 1);

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		var item = result.Value.Items[0];
		item.Description.Should().Be("A test recipe");
		item.Servings.Should().Be(4);
		item.PrepTimeMinutes.Should().Be(10);
		item.CookTimeMinutes.Should().Be(20);
		item.Difficulty.Should().Be("easy");
		item.ImageUrl.Should().Be("https://example.com/image.jpg");
	}

	[Fact]
	public async Task HandleAsync_RecipeWithoutOptionals_MapsNullFields()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Simple Recipe");
		SetupRepository([recipe], 1);

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		var item = result.Value.Items[0];
		item.Description.Should().BeNull();
		item.Servings.Should().BeNull();
		item.PrepTimeMinutes.Should().BeNull();
		item.CookTimeMinutes.Should().BeNull();
		item.Difficulty.Should().BeNull();
		item.ImageUrl.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_MultipleRecipes_MapsAllItems()
	{
		var recipe1 = RecipeTestData.CreateRecipe(title: "Recipe One");
		var recipe2 = RecipeTestData.CreateRecipe(title: "Recipe Two");
		var recipe3 = RecipeTestData.CreateRecipe(title: "Recipe Three");
		SetupRepository([recipe1, recipe2, recipe3], 3);

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Items.Should().HaveCount(3);
		result.Value.Items[0].Title.Should().Be("Recipe One");
		result.Value.Items[1].Title.Should().Be("Recipe Two");
		result.Value.Items[2].Title.Should().Be("Recipe Three");
	}

	[Fact]
	public async Task HandleAsync_PartialPage_ReturnsTotalCountLargerThanItems()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Only Recipe On Page");
		SetupRepository([recipe], 25);

		var query = CreateQuery(2, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Items.Should().HaveCount(1);
		result.Value.TotalCount.Should().Be(25);
	}

	[Fact]
	public async Task HandleAsync_FavoritedRecipe_MapsIsFavoriteTrue()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Pasta");
		SetupRepository([recipe], 1);
		favorites
			.GetFavoritedIdsAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<IReadOnlyCollection<RecipeIdentifier>>(), Arg.Any<CancellationToken>())
			.Returns((IReadOnlySet<Guid>)new HashSet<Guid> { recipe.Id.Value });

		var result = await handler.HandleAsync(CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending));

		result.Value.Items[0].IsFavorite.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_NotFavoritedRecipe_MapsIsFavoriteFalse()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Pasta");
		SetupRepository([recipe], 1);
		favorites
			.GetFavoritedIdsAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<IReadOnlyCollection<RecipeIdentifier>>(), Arg.Any<CancellationToken>())
			.Returns((IReadOnlySet<Guid>)new HashSet<Guid>());

		var result = await handler.HandleAsync(CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending));

		result.Value.Items[0].IsFavorite.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_MixedFavorites_OnlyFavoritedItemsHaveIsFavoriteTrue()
	{
		var fav = RecipeTestData.CreateRecipe(title: "Favorited");
		var notFav = RecipeTestData.CreateRecipe(title: "Not Favorited");
		SetupRepository([fav, notFav], 2);
		favorites
			.GetFavoritedIdsAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<IReadOnlyCollection<RecipeIdentifier>>(), Arg.Any<CancellationToken>())
			.Returns((IReadOnlySet<Guid>)new HashSet<Guid> { fav.Id.Value });

		var result = await handler.HandleAsync(CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending));

		result.Value.Items.Single(item => item.Identifier == fav.Id.Value).IsFavorite.Should().BeTrue();
		result.Value.Items.Single(item => item.Identifier == notFav.Id.Value).IsFavorite.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_QueriesFavoritesOncePerPage()
	{
		var r1 = RecipeTestData.CreateRecipe(title: "A");
		var r2 = RecipeTestData.CreateRecipe(title: "B");
		var r3 = RecipeTestData.CreateRecipe(title: "C");
		SetupRepository([r1, r2, r3], 3);

		await handler.HandleAsync(CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending));

		await favorites.Received(1).GetFavoritedIdsAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<IReadOnlyCollection<RecipeIdentifier>>(),
			Arg.Any<CancellationToken>());
	}
}
