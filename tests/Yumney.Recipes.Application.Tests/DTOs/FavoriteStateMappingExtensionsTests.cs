using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.DTOs;

public class FavoriteStateMappingExtensionsTests
{
	[Fact]
	public void ToFavoriteStateDto_True_ProjectsIdentifierAndFlag()
	{
		var recipe = RecipeIdentifier.New();

		var dto = recipe.ToFavoriteStateDto(isFavorite: true);

		dto.RecipeIdentifier.Should().Be(recipe.Value);
		dto.IsFavorite.Should().BeTrue();
	}

	[Fact]
	public void ToFavoriteStateDto_False_ProjectsIdentifierAndFlag()
	{
		var recipe = RecipeIdentifier.New();

		var dto = recipe.ToFavoriteStateDto(isFavorite: false);

		dto.RecipeIdentifier.Should().Be(recipe.Value);
		dto.IsFavorite.Should().BeFalse();
	}
}
