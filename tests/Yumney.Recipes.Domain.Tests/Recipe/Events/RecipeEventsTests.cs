using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe.Events;

/// <summary>
/// Construction + field-stamping tests for every Recipe domain event.
/// Records get free Equals / GetHashCode from the compiler; DomainEvent's
/// OccurredOn timestamp makes deep equality non-deterministic, so tests
/// assert per-property values instead.
/// </summary>
public class RecipeEventsTests
{
	private static readonly RecipeIdentifier RecipeId = RecipeIdentifier.New();
	private static readonly RecipeTitle Title = RecipeTitle.From("Carbonara");
	private static readonly OwnerIdentifier Owner = OwnerIdentifier.From("kc-user-1");

	[Fact]
	public void RecipeSavedEvent_PositionalCtor_StampsAllFields()
	{
		var @event = new RecipeSavedEvent(RecipeId, Title);

		@event.Recipe.Should().Be(RecipeId);
		@event.Title.Should().Be(Title);
	}

	[Fact]
	public void RecipeUpdatedEvent_PositionalCtor_StampsAllFields()
	{
		var @event = new RecipeUpdatedEvent(RecipeId, Title);

		@event.Recipe.Should().Be(RecipeId);
		@event.Title.Should().Be(Title);
	}

	[Fact]
	public void RecipeDeletedEvent_PositionalCtor_StampsAllFields()
	{
		var @event = new RecipeDeletedEvent(RecipeId, Title, Owner);

		@event.Recipe.Should().Be(RecipeId);
		@event.Title.Should().Be(Title);
		@event.Owner.Should().Be(Owner);
	}

	[Fact]
	public void RecipeRatedEvent_PositionalCtor_StampsAllFields()
	{
		var rating = Rating.From(4);

		var @event = new RecipeRatedEvent(RecipeId, rating);

		@event.Recipe.Should().Be(RecipeId);
		@event.Rating.Should().Be(rating);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void RecipeNotesUpdatedEvent_PositionalCtor_StampsAllFields(bool hasNotes)
	{
		var @event = new RecipeNotesUpdatedEvent(RecipeId, hasNotes);

		@event.Recipe.Should().Be(RecipeId);
		@event.HasNotes.Should().Be(hasNotes);
	}
}
