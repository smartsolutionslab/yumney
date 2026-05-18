using FluentAssertions;
using FluentAssertions.Execution;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

#pragma warning disable SA1601
public partial class RecipeTests
#pragma warning restore SA1601
{
	[Fact]
	public void Create_ValidInput_CreatesRecipeWithId()
	{
		var recipe = RecipeBuilder.A().Build();

		recipe.Id.Should().NotBeNull();
	}

	[Fact]
	public void Create_ValidInput_SetsTitle()
	{
		var recipe = RecipeBuilder.A().WithTitle("Pasta Carbonara").Build();

		recipe.Title.Should().Be(RecipeTitle.From("Pasta Carbonara"));
	}

	[Fact]
	public void Create_WithSourceUrl_SetsSourceUrl()
	{
		var recipe = RecipeBuilder.A().WithSourceUrl("https://example.com/recipe").Build();

		recipe.SourceUrl.Should().Be(RecipeUrl.From("https://example.com/recipe"));
	}

	[Fact]
	public void Create_ValidInput_SetsOwner()
	{
		var recipe = RecipeBuilder.A().OwnedBy("user-123").Build();

		recipe.Owner.Should().Be(OwnerIdentifier.From("user-123"));
	}

	[Fact]
	public void Create_ValidInput_SetsCreatedAtCloseToNow()
	{
		var before = DateTime.UtcNow;

		var recipe = RecipeBuilder.A().Build();

		recipe.CreatedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void Create_WithoutSourceUrl_SourceUrlIsNull()
	{
		var recipe = RecipeBuilder.A().Build();

		recipe.SourceUrl.Should().BeNull();
	}

	[Fact]
	public void Create_ValidInput_SetsIngredients()
	{
		var recipe = RecipeBuilder.A()
			.WithIngredients([
				IngredientBuilder.A().Named("Spaghetti").WithQuantity(400, Unit.Gram),
				IngredientBuilder.A().Named("Pancetta").WithQuantity(200, Unit.Gram),
			])
			.Build();

		recipe.Ingredients.Should().HaveCount(2);
	}

	[Fact]
	public void Create_ValidInput_SetsSteps()
	{
		var recipe = RecipeBuilder.A()
			.WithSteps([
				StepBuilder.A().Numbered(1).WithDescription("Cook pasta"),
				StepBuilder.A().Numbered(2).WithDescription("Fry pancetta"),
			])
			.Build();

		recipe.Steps.Should().HaveCount(2);
	}

	[Fact]
	public void Create_WithOptionalFields_SetsAllFields()
	{
		var recipe = RecipeBuilder.A()
			.WithDescription("A classic dish")
			.WithServings(4)
			.WithTiming(prepMinutes: 10, cookMinutes: 20)
			.WithDifficulty("medium")
			.WithImageUrl("https://example.com/image.jpg")
			.Build();

		using var scope = new AssertionScope();
		recipe.Description.Should().Be(RecipeDescription.From("A classic dish"));
		recipe.Servings.Should().Be(Servings.From(4));
		recipe.Timing?.Preparation.Should().Be(PreparationTime.From(10));
		recipe.Timing?.Cooking.Should().Be(CookingTime.From(20));
		recipe.Difficulty.Should().Be(Difficulty.From("medium"));
		recipe.ImageUrl.Should().Be(ImageUrl.From("https://example.com/image.jpg"));
	}

	[Fact]
	public void Create_WithoutOptionalFields_LeavesNullable()
	{
		var recipe = RecipeBuilder.A().Build();

		using var scope = new AssertionScope();
		recipe.Description.Should().BeNull();
		recipe.Servings.Should().BeNull();
		recipe.Timing?.Preparation.Should().BeNull();
		recipe.Timing?.Cooking.Should().BeNull();
		recipe.Difficulty.Should().BeNull();
		recipe.ImageUrl.Should().BeNull();
	}

	[Fact]
	public void Create_RaisesRecipeSavedEvent()
	{
		var recipe = RecipeBuilder.A().WithTitle("Pasta Carbonara").Build();

		recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeSavedEvent>()
			.Which.Title.Should().Be(RecipeTitle.From("Pasta Carbonara"));
	}

	[Fact]
	public void Create_GeneratesUniqueIds()
	{
		var recipe1 = RecipeBuilder.A().Build();
		var recipe2 = RecipeBuilder.A().Build();

		recipe1.Id.Should().NotBe(recipe2.Id);
	}

	[Fact]
	public void Create_EmptyIngredients_ThrowsGuardException()
	{
		// Cannot use the builder here — its Build() defaults to a non-empty
		// ingredients list specifically to keep the happy path concise.
		var act = () => Domain.Recipe.Recipe.Create(
			RecipeTitle.From("Test Recipe"),
			OwnerIdentifier.From("user-123"),
			ingredients: [],
			steps: [StepBuilder.A()],
			description: null,
			servings: null,
			timing: null,
			difficulty: null,
			imageUrl: null);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_EmptySteps_ThrowsGuardException()
	{
		var act = () => Domain.Recipe.Recipe.Create(
			RecipeTitle.From("Test Recipe"),
			OwnerIdentifier.From("user-123"),
			ingredients: [IngredientBuilder.A()],
			steps: [],
			description: null,
			servings: null,
			timing: null,
			difficulty: null,
			imageUrl: null);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_RecipeSavedEvent_ContainsRecipeId()
	{
		var recipe = RecipeBuilder.A().Build();

		var domainEvent = recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeSavedEvent>().Subject;

		domainEvent.Recipe.Should().Be(recipe.Id);
	}
}
