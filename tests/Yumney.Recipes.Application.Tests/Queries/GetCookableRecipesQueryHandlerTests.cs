using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Queries;

public class GetCookableRecipesQueryHandlerTests
{
	private const string OwnerId = "user-123";

	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IIngredientBalanceProvider balanceProvider = Substitute.For<IIngredientBalanceProvider>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetCookableRecipesQueryHandler handler;

	public GetCookableRecipesQueryHandlerTests()
	{
		currentUser.UserId.Returns(OwnerId);
		handler = new GetCookableRecipesQueryHandler(recipes, balanceProvider, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoRecipes_ReturnsEmptyList()
	{
		ConfigureBalance("salt", "pepper");
		ConfigureRecipes();

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_AllIngredientsAvailable_ReturnsFullMatch()
	{
		ConfigureBalance("flour", "eggs", "milk");
		ConfigureRecipes(MakeRecipe("Pancakes", "Flour", "Eggs", "Milk"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		var match = result.Value.Should().ContainSingle().Subject;
		match.Title.Should().Be("Pancakes");
		match.Tier.Should().Be(CookableRecipeMatchTier.Full);
		match.MissingIngredients.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_OneIngredientMissing_ReturnsNearMatchWithMissingItem()
	{
		ConfigureBalance("flour", "eggs");
		ConfigureRecipes(MakeRecipe("Pancakes", "Flour", "Eggs", "Milk"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		var match = result.Value.Should().ContainSingle().Subject;
		match.Tier.Should().Be(CookableRecipeMatchTier.Near);
		match.MissingIngredients.Should().BeEquivalentTo(["Milk"]);
	}

	[Fact]
	public async Task HandleAsync_MoreThanTwoMissing_RecipeIsExcluded()
	{
		ConfigureBalance("flour");
		ConfigureRecipes(MakeRecipe("Carbonara", "Pasta", "Egg", "Pancetta", "Cheese"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_FullMatchOnly_HidesNearMatches()
	{
		ConfigureBalance("flour", "eggs");
		ConfigureRecipes(
			MakeRecipe("Pancakes", "Flour", "Eggs", "Milk"),
			MakeRecipe("Crepes", "Flour", "Eggs"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery(FullMatchOnly: true));

		result.Value.Should().ContainSingle().Which.Title.Should().Be("Crepes");
	}

	[Fact]
	public async Task HandleAsync_StaplesCountAsAvailable()
	{
		ConfigureBalance("salt", "pepper", "tomato");
		ConfigureRecipes(MakeRecipe("Tomato Salt", "Tomato", "Salt", "Pepper"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Should().ContainSingle().Which.Tier.Should().Be(CookableRecipeMatchTier.Full);
	}

	[Fact]
	public async Task HandleAsync_MatchingIsCaseInsensitive()
	{
		ConfigureBalance("FLOUR", "EGGS");
		ConfigureRecipes(MakeRecipe("Crepes", "flour", "eggs"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Should().ContainSingle().Which.Tier.Should().Be(CookableRecipeMatchTier.Full);
	}

	[Fact]
	public async Task HandleAsync_RanksFullMatchesBeforeNear()
	{
		ConfigureBalance("flour", "eggs");
		ConfigureRecipes(
			MakeRecipe("Pancakes", "Flour", "Eggs", "Milk"),
			MakeRecipe("Crepes", "Flour", "Eggs"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("Crepes", "Pancakes");
	}

	[Fact]
	public async Task HandleAsync_WithinSameTier_FewerMissingFirst()
	{
		ConfigureBalance("flour");
		ConfigureRecipes(
			MakeRecipe("RecipeA", "Flour", "Eggs", "Milk"),
			MakeRecipe("RecipeB", "Flour", "Sugar"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("RecipeB", "RecipeA");
	}

	[Fact]
	public async Task HandleAsync_TiesBrokenAlphabetically()
	{
		ConfigureBalance("flour", "eggs", "milk");
		ConfigureRecipes(
			MakeRecipe("Zuppa", "Flour", "Eggs", "Milk"),
			MakeRecipe("Apple Pie", "Flour", "Eggs", "Milk"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("Apple Pie", "Zuppa");
	}

	[Fact]
	public async Task HandleAsync_WithinSameTier_RecipesUsingUseSoonOutrankFresh()
	{
		ConfigureBalance(("flour", Freshness.Fresh), ("milk", Freshness.UseSoon), ("eggs", Freshness.Fresh));
		ConfigureRecipes(
			MakeRecipe("FreshOnly", "Flour", "Eggs"),
			MakeRecipe("UsesMilk", "Flour", "Milk"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("UsesMilk", "FreshOnly");
	}

	[Fact]
	public async Task HandleAsync_MoreUrgentIngredients_RankedHigherWithinTier()
	{
		ConfigureBalance(
			("flour", Freshness.Fresh),
			("milk", Freshness.UseSoon),
			("chicken", Freshness.CheckIt));
		ConfigureRecipes(
			MakeRecipe("OneUrgent", "Flour", "Milk"),
			MakeRecipe("TwoUrgent", "Milk", "Chicken"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("TwoUrgent", "OneUrgent");
	}

	[Fact]
	public async Task HandleAsync_NotTrackedDoesNotCountAsUrgent()
	{
		ConfigureBalance(("salt", Freshness.NotTracked), ("milk", Freshness.UseSoon));
		ConfigureRecipes(
			MakeRecipe("StaplesOnly", "Salt"),
			MakeRecipe("UsesMilk", "Milk"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("UsesMilk", "StaplesOnly");
	}

	[Fact]
	public async Task HandleAsync_TierStillBeatsPerishability()
	{
		// A near-match with urgent ingredients does NOT beat a full match without any urgent items.
		ConfigureBalance(("flour", Freshness.Fresh), ("milk", Freshness.CheckIt));
		ConfigureRecipes(
			MakeRecipe("FullMatchFresh", "Flour"),
			MakeRecipe("NearMatchUrgent", "Milk", "Sugar"));

		var result = await handler.HandleAsync(new GetCookableRecipesQuery());

		result.Value.Select(r => r.Title).Should().Equal("FullMatchFresh", "NearMatchUrgent");
	}

	[Fact]
	public async Task HandleAsync_BalanceProviderQueriedWithCurrentUser()
	{
		ConfigureBalance(Array.Empty<string>());
		ConfigureRecipes();

		await handler.HandleAsync(new GetCookableRecipesQuery());

		await balanceProvider.Received(1).GetAvailableIngredientsAsync(OwnerId, Arg.Any<CancellationToken>());
	}

	private static Recipe MakeRecipe(string title, params string[] ingredientNames)
	{
		return Recipe.Create(
			RecipeTitle.From(title),
			OwnerIdentifier.From(OwnerId),
			ingredientNames.Select(n => Ingredient.Create(IngredientName.From(n), null)).ToList(),
			[Step.Create(StepNumber.From(1), StepDescription.From("Cook"))]);
	}

	private void ConfigureRecipes(params Recipe[] recipeList)
	{
		recipes.GetAllByOwnerWithIngredientsAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(recipeList);
	}

	private void ConfigureBalance(params string[] availableNames)
	{
		ConfigureBalance(availableNames.Select(n => (n, Freshness.Fresh)).ToArray());
	}

	private void ConfigureBalance(params (string Name, Freshness Freshness)[] items)
	{
		var dict = new Dictionary<string, Freshness>(StringComparer.OrdinalIgnoreCase);
		foreach (var (name, freshness) in items)
		{
			dict[name] = freshness;
		}

		balanceProvider.GetAvailableIngredientsAsync(OwnerId, Arg.Any<CancellationToken>())
			.Returns((IReadOnlyDictionary<string, Freshness>)dict);
	}
}
