using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipeFilterTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly PagingOptions DefaultPaging = PagingOptions.Of(Page.From(1), PageSize.From(20));
	private static readonly SortingOptions<RecipeSortField> DefaultSorting = new(RecipeSortField.Date, SortDirection.Descending);

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"filter-test-{Guid.NewGuid():N}");

	public async Task InitializeAsync()
	{
		await fixture.SeedRecipesAsync(
			BuildRecipe(
				title: "Quick Vegan Salad",
				tags: ["vegan", "quick", "salad"],
				difficulty: "easy",
				prepMinutes: 10,
				cookMinutes: 0),
			BuildRecipe(
				title: "Vegan Curry",
				tags: ["vegan", "indian"],
				difficulty: "medium",
				prepMinutes: 20,
				cookMinutes: 40),
			BuildRecipe(
				title: "Beef Wellington",
				tags: ["meat", "fancy"],
				difficulty: "hard",
				prepMinutes: 60,
				cookMinutes: 90),
			BuildRecipe(
				title: "Plain Rice",
				tags: null,
				difficulty: null,
				prepMinutes: null,
				cookMinutes: null));
	}

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateRecipesDbContextAsync,
		ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));

	[Fact]
	public async Task Filter_ByDifficulty_ReturnsOnlyMatching()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(Difficulty: Difficulty.From("easy"));

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(1);
		page.Items.Should().ContainSingle(r => r.Title.Value == "Quick Vegan Salad");
	}

	[Fact]
	public async Task Filter_ByMaxPrepTime_ReturnsRecipesAtOrUnderLimit()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(MaxPrepTime: PreparationTime.From(20));

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.Items.Select(recipe => recipe.Title.Value).Should()
			.Contain("Quick Vegan Salad")
			.And.Contain("Vegan Curry")
			.And.NotContain("Beef Wellington")
			.And.NotContain("Plain Rice");
	}

	[Fact]
	public async Task Filter_ByMaxCookTime_ReturnsRecipesAtOrUnderLimit()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(MaxCookTime: CookingTime.From(45));

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.Items.Select(recipe => recipe.Title.Value).Should()
			.Contain("Quick Vegan Salad")
			.And.Contain("Vegan Curry")
			.And.NotContain("Beef Wellington");
	}

	[Fact]
	public async Task Filter_BySingleTag_ReturnsRecipesWithTag()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(Tags: [RecipeTag.From("vegan")]);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(2);
		page.Items.Select(recipe => recipe.Title.Value).Should()
			.Contain("Quick Vegan Salad")
			.And.Contain("Vegan Curry");
	}

	[Fact]
	public async Task Filter_ByMultipleTags_RequiresAllTagsPresent()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(Tags: [RecipeTag.From("vegan"), RecipeTag.From("quick")]);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(1);
		page.Items.Should().ContainSingle(r => r.Title.Value == "Quick Vegan Salad");
	}

	[Fact]
	public async Task Filter_CombinesAllCriteria_WithAndLogic()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(
			Tags: [RecipeTag.From("vegan")],
			Difficulty: Difficulty.From("medium"),
			MaxPrepTime: PreparationTime.From(30),
			MaxCookTime: CookingTime.From(60));

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(1);
		page.Items.Should().ContainSingle(r => r.Title.Value == "Vegan Curry");
	}

	[Fact]
	public async Task Filter_NullFilter_ReturnsAllRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: null);

		page.TotalCount.Should().Be(4);
	}

	[Fact]
	public async Task Filter_EmptyFilter_ReturnsAllRecipes()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter();

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(4);
	}

	[Fact]
	public async Task Filter_NoMatch_ReturnsEmpty()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var filter = new RecipeFilter(Tags: [RecipeTag.From("dessert")]);

		var page = await recipes.GetByOwnerAsync(
			owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

		page.TotalCount.Should().Be(0);
		page.Items.Should().BeEmpty();
	}

	private Recipe BuildRecipe(
		string title,
		IReadOnlyList<string>? tags,
		string? difficulty,
		int? prepMinutes,
		int? cookMinutes)
	{
		var ingredients = new[]
		{
			Ingredient.Create(IngredientName.From("Test Ingredient"), Quantity.FromNullable(null, null)),
		};
		var steps = new[]
		{
			Step.Create(StepNumber.From(1), StepDescription.From("Test step")),
		};

		return Recipe.Create(
			RecipeTitle.From(title),
			owner,
			ingredients,
			steps,
			timing: TimingInfo.FromNullable(PreparationTime.FromNullable(prepMinutes), CookingTime.FromNullable(cookMinutes)),
			difficulty: Difficulty.FromNullable(difficulty),
			tags: tags?.Select(RecipeTag.From).ToList());
	}
}
