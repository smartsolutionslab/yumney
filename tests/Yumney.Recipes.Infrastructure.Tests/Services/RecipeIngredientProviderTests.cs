using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class RecipeIngredientProviderTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly RecipeIngredientProvider provider;

	public RecipeIngredientProviderTests()
	{
		provider = new RecipeIngredientProvider(recipes);
	}

	[Fact]
	public async Task GetIngredientsAsync_ReturnsIngredients()
	{
		var recipeId = Guid.NewGuid();
		SetupRecipe(
			[
				Ingredient.Create(IngredientName.From("Spaghetti"), Quantity.Of(Amount.From(500m), Unit.Gram)),
				Ingredient.Create(IngredientName.From("Tomato Sauce"), Quantity.Of(Amount.From(200m), Unit.Milliliter))
			],
			Servings.From(4));

		var result = await provider.GetIngredientsAsync(recipeId);

		result.Should().HaveCount(2);
		result[0].Name.Should().Be("Spaghetti");
		result[0].Amount.Should().Be(500m);
		result[0].Unit.Should().Be("g");
		result[0].RecipeServings.Should().Be(4);
	}

	[Fact]
	public async Task GetIngredientsAsync_IngredientWithoutQuantity_ReturnsNullAmountAndUnit()
	{
		var recipeId = Guid.NewGuid();
		SetupRecipe(
			[Ingredient.Create(IngredientName.From("Salt"), null)],
			Servings.From(2));

		var result = await provider.GetIngredientsAsync(recipeId);

		result.Should().HaveCount(1);
		result[0].Name.Should().Be("Salt");
		result[0].Amount.Should().BeNull();
		result[0].Unit.Should().BeNull();
	}

	[Fact]
	public async Task GetIngredientsAsync_RecipeWithNoServings_ReturnsNullServings()
	{
		var recipeId = Guid.NewGuid();
		SetupRecipe(
			[Ingredient.Create(IngredientName.From("Water"), Quantity.Of(Amount.From(500m), Unit.Milliliter))]);

		var result = await provider.GetIngredientsAsync(recipeId);

		result.Should().HaveCount(1);
		result[0].RecipeServings.Should().BeNull();
	}

	private void SetupRecipe(IReadOnlyList<Ingredient> ingredients, Servings? servings = null)
	{
		var step = Step.Create(StepNumber.From(1), StepDescription.From("Do something"));
		var recipe = Recipe.Create(
			RecipeTitle.From("Test Recipe"),
			OwnerIdentifier.From("user-123"),
			ingredients,
			[step],
			servings: servings);

		recipes.GetByIdAsync(Arg.Any<RecipeIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(recipe);
	}
}
