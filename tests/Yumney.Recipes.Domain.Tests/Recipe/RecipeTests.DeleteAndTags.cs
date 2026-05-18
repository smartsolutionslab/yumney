using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

#pragma warning disable SA1601
public partial class RecipeTests
#pragma warning restore SA1601
{
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

		domainEvent.Recipe.Should().Be(recipe.Id);
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
}
