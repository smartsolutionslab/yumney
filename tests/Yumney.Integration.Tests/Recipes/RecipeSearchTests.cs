using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipeSearchTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly PagingOptions DefaultPaging = PagingOptions.Of(Page.From(1), PageSize.From(20));
	private static readonly SortingOptions<RecipeSortField> DefaultSorting = new(RecipeSortField.Date, SortDirection.Descending);

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"search-test-{Guid.NewGuid():N}");

	public async Task InitializeAsync()
	{
		await fixture.SeedRecipesAsync(
			RecipeFactory.Lasagne(owner.Value),
			RecipeFactory.TomatoSoup(owner.Value),
			RecipeFactory.ChocolateCake(owner.Value));
	}

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateRecipesDbContextAsync,
		ctx => ctx.Recipes.Where(r => r.Owner == owner));

	[Fact]
	public async Task Search_ByTitle_FindsMatchingRecipe()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("lasagne"));

		totalCount.Value.Should().Be(1);
		items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByTitle_IsCaseInsensitive()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, _) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("LASAGNE"));

		items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByPartialTitle_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, _) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("choco"));

		items.Should().ContainSingle(r => r.Title.Value == "Chocolate Fudge Cake");
	}

	[Fact]
	public async Task Search_ByDescription_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, _) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("bolognese"));

		items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByIngredientName_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, _) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("mozzarella"));

		items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_SharedIngredient_FindsMultipleRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("butter"));

		totalCount.Value.Should().Be(2);
		items.Select(r => r.Title.Value).Should()
			.Contain("Classic Lasagne")
			.And.Contain("Chocolate Fudge Cake");
	}

	[Fact]
	public async Task Search_NoMatch_ReturnsEmpty()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("sushi"));

		totalCount.Value.Should().Be(0);
		items.Should().BeEmpty();
	}

	[Fact]
	public async Task Search_WithoutSearchTerm_ReturnsAllRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting);

		totalCount.Value.Should().Be(3);
		items.Should().HaveCount(3);
	}

	[Fact]
	public async Task Search_DifferentOwner_DoesNotReturnOtherUsersRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var otherOwner = OwnerIdentifier.From("nonexistent-user");

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			otherOwner, DefaultPaging, DefaultSorting, SearchTerm.From("lasagne"));

		totalCount.Value.Should().Be(0);
		items.Should().BeEmpty();
	}

	[Fact]
	public async Task Search_WithPagination_RespectsPageSize()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var paging = PagingOptions.Of(Page.From(1), PageSize.From(2));

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, paging, DefaultSorting);

		items.Should().HaveCount(2);
		totalCount.Value.Should().Be(3);
	}

	[Fact]
	public async Task Search_SortByNameAscending_ReturnsSorted()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var sorting = new SortingOptions<RecipeSortField>(RecipeSortField.Name, SortDirection.Ascending);

		var (items, _) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, sorting);

		items.Select(r => r.Title.Value).Should()
			.ContainInOrder("Chocolate Fudge Cake", "Classic Lasagne", "Roasted Tomato Soup");
	}

	[Fact]
	public async Task Search_ByTomato_FindsTitleAndIngredientMatches()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("tomato"));

		totalCount.Value.Should().BeGreaterThanOrEqualTo(2);
		items.Select(r => r.Title.Value).Should()
			.Contain("Roasted Tomato Soup")
			.And.Contain("Classic Lasagne");
	}
}
