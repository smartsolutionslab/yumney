using FluentAssertions;
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
	public void Update_ValidInput_SetsTitle()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Title.Should().Be(RecipeTitle.From("Updated Recipe"));
	}

	[Fact]
	public void Update_ValidInput_SetsDescription()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Description.Should().Be(RecipeDescription.From("Updated description"));
	}

	[Fact]
	public void Update_ValidInput_SetsServings()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Servings.Should().Be(Servings.From(6));
	}

	[Fact]
	public void Update_ValidInput_SetsPreparationTime()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Timing?.Preparation.Should().Be(PreparationTime.From(15));
	}

	[Fact]
	public void Update_ValidInput_SetsCookingTime()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Timing?.Cooking.Should().Be(CookingTime.From(30));
	}

	[Fact]
	public void Update_ValidInput_SetsDifficulty()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.Difficulty.Should().Be(Difficulty.From("hard"));
	}

	[Fact]
	public void Update_ValidInput_SetsImageUrl()
	{
		var recipe = CreateRecipeWithUpdatedFields();

		recipe.ImageUrl.Should().Be(ImageUrl.From("https://example.com/new.jpg"));
	}

	[Fact]
	public void Update_ReplacesIngredients()
	{
		var recipe = RecipeBuilder.A()
			.WithIngredients([
				IngredientBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				IngredientBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram),
			])
			.Build();

		var butterName = IngredientName.From("Butter");
		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A().Named(butterName).WithQuantity(100, Unit.Gram)],
			[StepBuilder.A().WithDescription("Mix")]);

		recipe.Ingredients.Should().HaveCount(1);
		recipe.Ingredients[0].Name.Should().Be(butterName);
	}

	[Fact]
	public void Update_ReplacesSteps()
	{
		var recipe = RecipeBuilder.A()
			.WithSteps([
				StepBuilder.A().Numbered(1).WithDescription("Step one"),
				StepBuilder.A().Numbered(2).WithDescription("Step two"),
			])
			.Build();

		var newDescription = StepDescription.From("New only step");
		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A().Numbered(1).WithDescription(newDescription)]);

		recipe.Steps.Should().HaveCount(1);
		recipe.Steps[0].Description.Should().Be(newDescription);
	}

	[Fact]
	public void Update_RaisesRecipeUpdatedEvent()
	{
		var recipe = RecipeBuilder.A().Build();
		recipe.ClearDomainEvents();

		var newTitle = RecipeTitle.From("Updated Recipe");
		recipe.Update(newTitle, [IngredientBuilder.A()], [StepBuilder.A()]);

		recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeUpdatedEvent>()
			.Which.Title.Should().Be(newTitle);
	}

	[Fact]
	public void Update_RecipeUpdatedEvent_ContainsRecipeId()
	{
		var recipe = RecipeBuilder.A().Build();
		recipe.ClearDomainEvents();

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		var domainEvent = recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeUpdatedEvent>().Subject;

		domainEvent.Recipe.Should().Be(recipe.Id);
	}

	[Fact]
	public void Update_EmptyIngredients_ThrowsGuardException()
	{
		var recipe = RecipeBuilder.A().Build();

		var act = () => recipe.Update(
			RecipeTitle.From("Updated"),
			[],
			[StepBuilder.A()]);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Update_EmptySteps_ThrowsGuardException()
	{
		var recipe = RecipeBuilder.A().Build();

		var act = () => recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[]);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Update_DoesNotChangeOwner()
	{
		var owner = OwnerIdentifier.From("user-123");
		var recipe = RecipeBuilder.A().OwnedBy(owner).Build();

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		recipe.Owner.Should().Be(owner);
	}

	[Fact]
	public void Update_DoesNotChangeSourceUrl()
	{
		var sourceUrl = RecipeUrl.From("https://example.com/recipe");
		var recipe = RecipeBuilder.A().WithSourceUrl(sourceUrl).Build();

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		recipe.SourceUrl.Should().Be(sourceUrl);
	}

	[Fact]
	public void Update_DoesNotChangeCreatedAt()
	{
		var recipe = RecipeBuilder.A().Build();
		var originalCreatedAt = recipe.CreatedAt;

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		recipe.CreatedAt.Should().Be(originalCreatedAt);
	}

	[Fact]
	public void Update_WithoutOptionalFields_ClearsExistingValues()
	{
		var recipe = RecipeBuilder.A()
			.WithDescription("Old")
			.WithServings(4)
			.WithTiming(prepMinutes: 10, cookMinutes: 20)
			.WithDifficulty("easy")
			.WithImageUrl("https://example.com/old.jpg")
			.Build();

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		recipe.Description.Should().BeNull();
		recipe.Servings.Should().BeNull();
		recipe.Timing?.Preparation.Should().BeNull();
		recipe.Timing?.Cooking.Should().BeNull();
		recipe.Difficulty.Should().BeNull();
		recipe.ImageUrl.Should().BeNull();
	}

	private static Domain.Recipe.Recipe CreateRecipeWithUpdatedFields()
	{
		var recipe = RecipeBuilder.A()
			.WithDescription("Old description")
			.WithServings(2)
			.Build();

		recipe.Update(
			RecipeTitle.From("Updated Recipe"),
			[IngredientBuilder.A().Named("Butter").WithQuantity(100, Unit.Gram)],
			[StepBuilder.A().Numbered(1).WithDescription("Melt butter")],
			RecipeDescription.From("Updated description"),
			Servings.From(6),
			TimingInfoBuilder.A().WithPreparationMinutes(15).WithCookingMinutes(30),
			Difficulty.From("hard"),
			ImageUrl.From("https://example.com/new.jpg"));

		return recipe;
	}
}
