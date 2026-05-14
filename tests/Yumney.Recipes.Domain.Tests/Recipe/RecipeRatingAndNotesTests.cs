using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeRatingAndNotesTests
{
	[Fact]
	public void NewRecipe_HasNoRatingOrNotes()
	{
		var recipe = CreateRecipe();

		recipe.Rating.Should().BeNull();
		recipe.Notes.Should().BeNull();
	}

	[Fact]
	public void RateAs_StoresTheRating()
	{
		var recipe = CreateRecipe();

		recipe.RateAs(Rating.From(4));

		recipe.Rating!.Value.Should().Be(4);
	}

	[Fact]
	public void RateAs_RaisesRecipeRatedEvent()
	{
		var recipe = CreateRecipe();

		recipe.RateAs(Rating.From(5));

		var raised = recipe.DomainEvents.OfType<RecipeRatedEvent>().Single();
		raised.Recipe.Should().Be(recipe.Id);
		raised.Rating.Value.Should().Be(5);
	}

	[Fact]
	public void RateAs_OverridesPreviousRating()
	{
		var recipe = CreateRecipe();
		recipe.RateAs(Rating.From(2));

		recipe.RateAs(Rating.From(5));

		recipe.Rating!.Value.Should().Be(5);
	}

	[Fact]
	public void UpdateNotes_StoresTheNotes()
	{
		var recipe = CreateRecipe();

		recipe.UpdateNotes(Notes.From("Add more garlic"));

		recipe.Notes!.Value.Should().Be("Add more garlic");
	}

	[Fact]
	public void UpdateNotes_NullClearsTheNotes()
	{
		var recipe = CreateRecipe();
		recipe.UpdateNotes(Notes.From("Original"));

		recipe.UpdateNotes(null);

		recipe.Notes.Should().BeNull();
	}

	[Fact]
	public void UpdateNotes_RaisesEventReflectingPresence()
	{
		var recipe = CreateRecipe();

		recipe.UpdateNotes(Notes.From("Salty"));

		var raised = recipe.DomainEvents.OfType<RecipeNotesUpdatedEvent>().Last();
		raised.Recipe.Should().Be(recipe.Id);
		raised.HasNotes.Should().BeTrue();
	}

	[Fact]
	public void UpdateNotes_NullEventReflectsAbsence()
	{
		var recipe = CreateRecipe();

		recipe.UpdateNotes(null);

		var raised = recipe.DomainEvents.OfType<RecipeNotesUpdatedEvent>().Last();
		raised.HasNotes.Should().BeFalse();
	}

	private static Domain.Recipe.Recipe CreateRecipe() =>
		RecipeBuilder.A()
			.WithIngredients([IngredientBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram)])
			.Build();
}
