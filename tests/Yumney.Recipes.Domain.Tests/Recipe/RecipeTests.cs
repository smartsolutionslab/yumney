using FluentAssertions;
using FluentAssertions.Execution;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeTests
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

		domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
	}

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

		domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
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

	[Fact]
	public void MarkAsDeleted_RaisesRecipeDeletedEvent()
	{
		var recipe = RecipeBuilder.A().Build();
		recipe.ClearDomainEvents();

		recipe.MarkAsDeleted();

		recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeDeletedEvent>();
	}

	[Fact]
	public void MarkAsDeleted_EventContainsRecipeIdentifier()
	{
		var recipe = RecipeBuilder.A().Build();
		recipe.ClearDomainEvents();

		recipe.MarkAsDeleted();

		var domainEvent = recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

		domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
	}

	[Fact]
	public void MarkAsDeleted_EventContainsTitle()
	{
		var title = RecipeTitle.From("Pasta Carbonara");
		var recipe = RecipeBuilder.A().WithTitle(title).Build();
		recipe.ClearDomainEvents();

		recipe.MarkAsDeleted();

		var domainEvent = recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

		domainEvent.Title.Should().Be(title);
	}

	[Fact]
	public void MarkAsDeleted_EventContainsOwner()
	{
		var owner = OwnerIdentifier.From("user-123");
		var recipe = RecipeBuilder.A().OwnedBy(owner).Build();
		recipe.ClearDomainEvents();

		recipe.MarkAsDeleted();

		var domainEvent = recipe.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

		domainEvent.Owner.Should().Be(owner);
	}

	[Fact]
	public void Create_WithTags_SetsTags()
	{
		var italianTag = RecipeTag.From("italian");
		var pastaTag = RecipeTag.From("pasta");
		var recipe = RecipeBuilder.A().WithTags([italianTag, pastaTag]).Build();

		recipe.Tags.Should().HaveCount(2);
		recipe.Tags[0].Should().Be(italianTag);
		recipe.Tags[1].Should().Be(pastaTag);
	}

	[Fact]
	public void Create_WithoutTags_TagsEmpty()
	{
		var recipe = RecipeBuilder.A().Build();

		recipe.Tags.Should().BeEmpty();
	}

	[Fact]
	public void Update_WithTags_ReplacesTags()
	{
		var recipe = RecipeBuilder.A().WithTag("old-tag").Build();

		var newTag = RecipeTag.From("new-tag");
		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()],
			tags: [newTag]);

		recipe.Tags.Should().ContainSingle().Which.Should().Be(newTag);
	}

	[Fact]
	public void Update_WithNullTags_ClearsTags()
	{
		var recipe = RecipeBuilder.A().WithTag("old-tag").Build();

		recipe.Update(
			RecipeTitle.From("Updated"),
			[IngredientBuilder.A()],
			[StepBuilder.A()]);

		recipe.Tags.Should().BeEmpty();
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
