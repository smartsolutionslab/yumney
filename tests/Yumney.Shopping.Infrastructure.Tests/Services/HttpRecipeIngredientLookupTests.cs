using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Client;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Services;

public class HttpRecipeIngredientLookupTests
{
	private static readonly RecipeReference TestRecipe = RecipeReference.New();

	[Fact]
	public async Task LookupAsync_NullResponse_ReturnsEmpty()
	{
		var lookup = CreateLookup(response: null);

		var result = await lookup.LookupAsync(TestRecipe);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task LookupAsync_ProjectsServingsOntoEveryIngredient()
	{
		var lookup = CreateLookup(new RecipeResponse(
			Servings: 2,
			Ingredients:
			[
				new RecipeIngredientPayload("Pasta", 250m, "g"),
				new RecipeIngredientPayload("Cheese", 100m, "g"),
			]));

		var result = await lookup.LookupAsync(TestRecipe);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(ingredient => ingredient.RecipeServings.Should().Be(2));
	}

	[Fact]
	public async Task LookupAsync_NullServings_PropagatesNull()
	{
		var lookup = CreateLookup(new RecipeResponse(
			Servings: null,
			Ingredients: [new RecipeIngredientPayload("Pepper", 1m, "tsp")]));

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
