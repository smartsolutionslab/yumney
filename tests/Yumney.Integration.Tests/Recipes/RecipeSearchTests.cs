using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Paging;
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
		ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));

	[Fact]
	public async Task Search_ByTitle_FindsMatchingRecipe()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("lasagne"));

		page.TotalCount.Should().Be(1);
		page.Items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByTitle_IsCaseInsensitive()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("LASAGNE"));

		page.Items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByPartialTitle_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("choco"));

		page.Items.Should().ContainSingle(r => r.Title.Value == "Chocolate Fudge Cake");
	}

	[Fact]
	public async Task Search_ByDescription_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("bolognese"));

		page.Items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_ByIngredientName_FindsMatch()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("mozzarella"));

		page.Items.Should().ContainSingle(r => r.Title.Value == "Classic Lasagne");
	}

	[Fact]
	public async Task Search_SharedIngredient_FindsMultipleRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("butter"));

		page.TotalCount.Should().Be(2);
		page.Items.Select(recipe => recipe.Title.Value).Should()
			.Contain("Classic Lasagne")
			.And.Contain("Chocolate Fudge Cake");
	}

	[Fact]
	public async Task Search_NoMatch_ReturnsEmpty()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("sushi"));

		page.TotalCount.Should().Be(0);
		page.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task Search_WithoutSearchTerm_ReturnsAllRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting);

		page.TotalCount.Should().Be(3);
		page.Items.Should().HaveCount(3);
	}

	[Fact]
	public async Task Search_DifferentOwner_DoesNotReturnOtherUsersRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var otherOwner = OwnerIdentifier.From("nonexistent-user");

		var page = await recipes.GetByOwnerAsync(
			otherOwner, DefaultPaging, DefaultSorting, SearchTerm.From("lasagne"));

		page.TotalCount.Should().Be(0);
		page.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task Search_WithPagination_RespectsPageSize()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var paging = PagingOptions.Of(Page.From(1), PageSize.From(2));

		var page = await recipes.GetByOwnerAsync(
			owner, paging, DefaultSorting);

		page.Items.Should().HaveCount(2);
		page.TotalCount.Should().Be(3);
	}

	[Fact]
	public async Task Search_SortByNameAscending_ReturnsSorted()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var sorting = new SortingOptions<RecipeSortField>(RecipeSortField.Name, SortDirection.Ascending);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, sorting);

		page.Items.Select(recipe => recipe.Title.Value).Should()
			.ContainInOrder("Chocolate Fudge Cake", "Classic Lasagne", "Roasted Tomato Soup");
	}

	[Fact]
	public async Task Search_ByTomato_FindsTitleAndIngredientMatches()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, SearchTerm.From("tomato"));

		page.TotalCount.Should().BeGreaterThanOrEqualTo(2);
		page.Items.Select(recipe => recipe.Title.Value).Should()
			.Contain("Roasted Tomato Soup")
			.And.Contain("Classic Lasagne");
	}
}
