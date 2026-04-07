using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Common;
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
        ctx => ctx.Recipes.Where(r => r.Owner == owner));

    [Fact]
    public async Task Filter_ByDifficulty_ReturnsOnlyMatching()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);
        var filter = new RecipeFilter(Difficulty: Difficulty.From("easy"));

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(1);
        items.Should().ContainSingle(r => r.Title.Value == "Quick Vegan Salad");
    }

    [Fact]
    public async Task Filter_ByMaxPrepTime_ReturnsRecipesAtOrUnderLimit()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);
        var filter = new RecipeFilter(MaxPrepTime: PreparationTime.From(20));

        var (items, _) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        items.Select(r => r.Title.Value).Should()
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

        var (items, _) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        items.Select(r => r.Title.Value).Should()
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

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(2);
        items.Select(r => r.Title.Value).Should()
            .Contain("Quick Vegan Salad")
            .And.Contain("Vegan Curry");
    }

    [Fact]
    public async Task Filter_ByMultipleTags_RequiresAllTagsPresent()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);
        var filter = new RecipeFilter(Tags: [RecipeTag.From("vegan"), RecipeTag.From("quick")]);

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(1);
        items.Should().ContainSingle(r => r.Title.Value == "Quick Vegan Salad");
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

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(1);
        items.Should().ContainSingle(r => r.Title.Value == "Vegan Curry");
    }

    [Fact]
    public async Task Filter_NullFilter_ReturnsAllRecipes()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);

        var (_, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: null);

        totalCount.Value.Should().Be(4);
    }

    [Fact]
    public async Task Filter_EmptyFilter_ReturnsAllRecipes()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);
        var filter = new RecipeFilter();

        var (_, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(4);
    }

    [Fact]
    public async Task Filter_NoMatch_ReturnsEmpty()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);
        var filter = new RecipeFilter(Tags: [RecipeTag.From("dessert")]);

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, DefaultPaging, DefaultSorting, search: null, filter: filter);

        totalCount.Value.Should().Be(0);
        items.Should().BeEmpty();
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
            preparationTime: PreparationTime.FromNullable(prepMinutes),
            cookingTime: CookingTime.FromNullable(cookMinutes),
            difficulty: Difficulty.FromNullable(difficulty),
            tags: tags?.Select(RecipeTag.From).ToList());
    }
}
