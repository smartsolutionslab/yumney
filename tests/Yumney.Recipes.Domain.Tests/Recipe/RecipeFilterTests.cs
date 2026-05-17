using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeFilterTests
{
	[Fact]
	public void DefaultCtor_AllNullsOrFalseEquivalents_IsEmpty()
	{
		var filter = new RecipeFilter();

		filter.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void EmptyTagsList_CountsAsEmpty()
	{
		var filter = new RecipeFilter(Tags: []);

		filter.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void WithTags_IsNotEmpty()
	{
		var filter = new RecipeFilter(Tags: [RecipeTag.From("italian")]);

		filter.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void WithDifficulty_IsNotEmpty()
	{
		var filter = new RecipeFilter(Difficulty: Difficulty.From("easy"));

		filter.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void WithMaxPrepTime_IsNotEmpty()
	{
		var filter = new RecipeFilter(MaxPrepTime: PreparationTime.From(15));

		filter.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void WithMaxCookTime_IsNotEmpty()
	{
		var filter = new RecipeFilter(MaxCookTime: CookingTime.From(30));

		filter.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void WithFavoritesOnlyTrue_IsNotEmpty()
	{
		var filter = new RecipeFilter(FavoritesOnly: true);

		filter.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void WithFavoritesOnlyFalse_IsStillEmpty()
	{
		// FavoritesOnly = false means "don't filter by favorites" — semantically
		// equivalent to null, so IsEmpty stays true. Only `true` activates the
		// filter.
		var filter = new RecipeFilter(FavoritesOnly: false);

		filter.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void Equality_SameFields_AreEqual()
	{
		var a = new RecipeFilter(Difficulty: Difficulty.From("medium"), MaxPrepTime: PreparationTime.From(20));
		var b = new RecipeFilter(Difficulty: Difficulty.From("medium"), MaxPrepTime: PreparationTime.From(20));

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
