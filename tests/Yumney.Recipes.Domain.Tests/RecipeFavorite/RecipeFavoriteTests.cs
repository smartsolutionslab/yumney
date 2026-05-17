using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.RecipeFavorite;

public class RecipeFavoriteTests
{
	[Fact]
	public void Create_StampsRecipeOwnerAndFavoritedAt()
	{
		var recipe = RecipeIdentifier.From(Guid.NewGuid());
		var owner = OwnerIdentifier.From("kc-user-1");
		var before = DateTime.UtcNow;

		var favorite = global::SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite.RecipeFavorite.Create(recipe, owner);

		favorite.Recipe.Should().Be(recipe);
		favorite.Owner.Should().Be(owner);
		favorite.FavoritedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
		favorite.Id.Should().NotBe(default(RecipeFavoriteIdentifier));
	}

	[Fact]
	public void Create_NullRecipe_Throws()
	{
		var owner = OwnerIdentifier.From("kc-user-1");

		var act = () => global::SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite.RecipeFavorite.Create(null!, owner);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_NullOwner_Throws()
	{
		var recipe = RecipeIdentifier.From(Guid.NewGuid());

		var act = () => global::SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite.RecipeFavorite.Create(recipe, null!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_TwoCalls_ProduceDistinctIdentifiers()
	{
		var recipe = RecipeIdentifier.From(Guid.NewGuid());
		var owner = OwnerIdentifier.From("kc-user-1");

		var first = global::SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite.RecipeFavorite.Create(recipe, owner);
		var second = global::SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite.RecipeFavorite.Create(recipe, owner);

		first.Id.Should().NotBe(second.Id);
	}
}
