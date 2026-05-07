using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpRecipeIngredientLookupTests
{
	private static readonly SlotRecipeIdentifier TestRecipe = SlotRecipeIdentifier.New();

	[Fact]
	public async Task LookupAsync_NullResponse_ReturnsEmpty()
	{
		var lookup = CreateLookup(response: null);

		var result = await lookup.LookupAsync(TestRecipe);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task LookupAsync_EmptyIngredients_ReturnsEmpty()
	{
		var lookup = CreateLookup(new RecipeResponse(4, []));

		var result = await lookup.LookupAsync(TestRecipe);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task LookupAsync_ProjectsServingsOntoEveryIngredient()
	{
		var lookup = CreateLookup(new RecipeResponse(
			Servings: 4,
			Ingredients:
			[
				new RecipeIngredientPayload("Tomato", 200m, "g"),
				new RecipeIngredientPayload("Olive Oil", 30m, "ml"),
			]));

		var result = await lookup.LookupAsync(TestRecipe);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(ingredient => ingredient.RecipeServings.Should().Be(4));
		result[0].Name.Should().Be("Tomato");
		result[0].Amount.Should().Be(200m);
		result[0].Unit.Should().Be("g");
	}

	[Fact]
	public async Task LookupAsync_NullServings_PropagatesNull()
	{
		var lookup = CreateLookup(new RecipeResponse(
			Servings: null,
			Ingredients: [new RecipeIngredientPayload("Salt", 1m, "tsp")]));

		var result = await lookup.LookupAsync(TestRecipe);

		result[0].RecipeServings.Should().BeNull();
	}

	private static HttpRecipeIngredientLookup CreateLookup(RecipeResponse? response)
	{
		var recipes = Substitute.For<IRecipesClient>();
		recipes.GetRecipeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(response);
		return new HttpRecipeIngredientLookup(recipes);
	}
}
