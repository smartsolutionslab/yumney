using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.DTOs;

public class RecipeMappingExtensionsTests
{
	[Fact]
	public void ToDetailDto_FullRecipe_ProjectsEveryField()
	{
		var recipe = RecipeBuilder.A()
			.WithTitle("Carbonara")
			.WithDescription("Roman classic")
			.WithServings(4)
			.WithTiming(prepMinutes: 10, cookMinutes: 20)
			.WithDifficulty("easy")
			.WithImageUrl("https://example.com/image.jpg")
			.WithLanguage("en")
			.WithSourceUrl("https://example.com/recipe")
			.WithTag("italian")
			.Build();
		recipe.RateAs(Rating.From(5));
		recipe.UpdateNotes(Notes.From("Use guanciale"));

		var dto = recipe.ToDetailDto(isFavorite: true);

		dto.Identifier.Should().Be(recipe.Id.Value);
		dto.Title.Should().Be("Carbonara");
		dto.Description.Should().Be("Roman classic");
		dto.Servings.Should().Be(4);
		dto.PrepTimeMinutes.Should().Be(10);
		dto.CookTimeMinutes.Should().Be(20);
		dto.Difficulty.Should().Be("easy");
		dto.ImageUrl.Should().Be("https://example.com/image.jpg");
		dto.Language.Should().Be("en");
		dto.SourceUrl.Should().Be("https://example.com/recipe");
		dto.Tags.Should().ContainSingle().Which.Should().Be("italian");
		dto.IsFavorite.Should().BeTrue();
		dto.Rating.Should().Be(5);
		dto.Notes.Should().Be("Use guanciale");
		dto.Ingredients.Should().NotBeEmpty();
		dto.Steps.Should().NotBeEmpty();
	}

	[Fact]
	public void ToDetailDto_MinimalRecipe_LeavesOptionalFieldsNull()
	{
		var recipe = RecipeBuilder.A().WithTitle("Bare").Build();

		var dto = recipe.ToDetailDto();

		dto.Description.Should().BeNull();
		dto.Servings.Should().BeNull();
		dto.PrepTimeMinutes.Should().BeNull();
		dto.CookTimeMinutes.Should().BeNull();
		dto.Difficulty.Should().BeNull();
		dto.ImageUrl.Should().BeNull();
		dto.Language.Should().BeNull();
		dto.SourceUrl.Should().BeNull();
		dto.Rating.Should().BeNull();
		dto.Notes.Should().BeNull();
		dto.Tags.Should().BeEmpty();
		dto.IsFavorite.Should().BeFalse();
	}

	[Fact]
	public void ToListItemDto_RecipeWithNotes_HasNotesIsTrue()
	{
		var recipe = RecipeBuilder.A().WithTitle("Stew").Build();
		recipe.UpdateNotes(Notes.From("Reduce salt"));

		var dto = recipe.ToListItemDto(isFavorite: true);

		dto.HasNotes.Should().BeTrue();
		dto.IsFavorite.Should().BeTrue();
		dto.Title.Should().Be("Stew");
	}

	[Fact]
	public void ToListItemDto_RecipeWithoutNotes_HasNotesIsFalse()
	{
		var recipe = RecipeBuilder.A().Build();

		var dto = recipe.ToListItemDto();

		dto.HasNotes.Should().BeFalse();
		dto.IsFavorite.Should().BeFalse();
	}

	[Fact]
	public void ToSavedDto_ProjectsIdentifierTitleAndCreatedAt()
	{
		var recipe = RecipeBuilder.A().WithTitle("Risotto").Build();

		var dto = recipe.ToSavedDto();

		dto.Identifier.Should().Be(recipe.Id.Value);
		dto.Title.Should().Be("Risotto");
		dto.CreatedAt.Should().Be(recipe.CreatedAt);
	}

	[Fact]
	public void ToCookableDto_FullMatch_HasEmptyMissingList()
	{
		var recipe = RecipeBuilder.A()
			.WithTitle("Soup")
			.WithServings(2)
			.WithTiming(prepMinutes: 5, cookMinutes: 30)
			.WithDifficulty("medium")
			.WithImageUrl("https://example.com/soup.jpg")
			.Build();

		var dto = recipe.ToCookableDto(CookableRecipeMatchTier.Full, missingIngredients: []);

		dto.RecipeIdentifier.Should().Be(recipe.Id.Value);
		dto.Title.Should().Be("Soup");
		dto.ImageUrl.Should().Be("https://example.com/soup.jpg");
		dto.Servings.Should().Be(2);
		dto.PrepTimeMinutes.Should().Be(5);
		dto.CookTimeMinutes.Should().Be(30);
		dto.Difficulty.Should().Be("medium");
		dto.IngredientCount.Should().Be(recipe.Ingredients.Count);
		dto.Tier.Should().Be(CookableRecipeMatchTier.Full);
		dto.MissingIngredients.Should().BeEmpty();
	}

	[Fact]
	public void ToCookableDto_NearMatch_PreservesMissingList()
	{
		var recipe = RecipeBuilder.A().WithTitle("Stir-fry").Build();

		var dto = recipe.ToCookableDto(
			CookableRecipeMatchTier.Near,
			missingIngredients: ["soy sauce", "ginger"]);

		dto.Tier.Should().Be(CookableRecipeMatchTier.Near);
		dto.MissingIngredients.Should().Equal("soy sauce", "ginger");
	}

	[Fact]
	public void IngredientToDto_WithQuantity_ProjectsAmountAndUnit()
	{
		Ingredient ingredient = IngredientBuilder.A().Named("Flour").WithQuantity(500m, Unit.Gram);

		var dto = ingredient.ToDto();

		dto.Name.Should().Be("Flour");
		dto.Amount.Should().Be(500m);
		dto.Unit.Should().Be(Unit.Gram.Value);
	}

	[Fact]
	public void IngredientToDto_WithoutQuantity_LeavesAmountAndUnitNull()
	{
		Ingredient ingredient = IngredientBuilder.A().Named("Salt");

		var dto = ingredient.ToDto();

		dto.Name.Should().Be("Salt");
		dto.Amount.Should().BeNull();
		dto.Unit.Should().BeNull();
	}

	[Fact]
	public void IngredientsToDtos_MapsEveryIngredient()
	{
		Ingredient[] ingredients =
		[
			IngredientBuilder.A().Named("Flour").WithQuantity(500m, Unit.Gram),
			IngredientBuilder.A().Named("Eggs"),
		];

		var dtos = ingredients.ToDtos();

		dtos.Should().HaveCount(2);
		dtos[0].Name.Should().Be("Flour");
		dtos[1].Name.Should().Be("Eggs");
	}

	[Fact]
	public void StepToDto_ProjectsNumberAndDescription()
	{
		Step step = StepBuilder.A().Numbered(2).WithDescription("Mix");

		var dto = step.ToDto();

		dto.Number.Should().Be(2);
		dto.Description.Should().Be("Mix");
	}

	[Fact]
	public void StepsToDtos_MapsEveryStep()
	{
		Step[] steps =
		[
			StepBuilder.A().Numbered(1).WithDescription("Chop"),
			StepBuilder.A().Numbered(2).WithDescription("Saute"),
		];

		var dtos = steps.ToDtos();

		dtos.Should().HaveCount(2);
		dtos[0].Number.Should().Be(1);
		dtos[1].Description.Should().Be("Saute");
	}
}
