using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class IngredientIdentifierTests
{
	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();

		var identifier = IngredientIdentifier.From(guid);

		identifier.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => IngredientIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void New_GeneratesUniqueId()
	{
		var first = IngredientIdentifier.New();
		var second = IngredientIdentifier.New();

		first.Should().NotBe(second);
	}

	[Fact]
	public void New_IsNotEmpty()
	{
		var identifier = IngredientIdentifier.New();

		identifier.Value.Should().NotBeEmpty();
	}

	[Fact]
	public void ToString_ReturnsGuidString()
	{
		var guid = Guid.NewGuid();

		var identifier = IngredientIdentifier.From(guid);

		identifier.ToString().Should().Be(guid.ToString());
	}
}
