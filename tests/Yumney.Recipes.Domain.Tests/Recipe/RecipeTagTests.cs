using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeTagTests
{
	[Fact]
	public void From_ValidTag_CreatesInstance()
	{
		var tag = RecipeTag.From("vegetarian");

		tag.Value.Should().Be("vegetarian");
	}

	[Fact]
	public void From_TrimsWhitespace()
	{
		var tag = RecipeTag.From("  vegetarian  ");

		tag.Value.Should().Be("vegetarian");
	}

	[Fact]
	public void From_ConvertsToLowerCase()
	{
		var tag = RecipeTag.From("Vegetarian");

		tag.Value.Should().Be("vegetarian");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => RecipeTag.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_ExceedsMaxLength_ThrowsGuardException()
	{
		var value = new string('a', 51);

		var act = () => RecipeTag.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_AtMaxLength_CreatesInstance()
	{
		var value = new string('a', 50);

		var tag = RecipeTag.From(value);

		tag.Value.Should().HaveLength(50);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var tag1 = RecipeTag.From("vegetarian");
		var tag2 = RecipeTag.From("vegetarian");

		tag1.Should().Be(tag2);
	}

	[Fact]
	public void ImplicitConversion_ReturnsValue()
	{
		var tag = RecipeTag.From("vegetarian");

		string result = (string)tag;

		result.Should().Be("vegetarian");
	}
}
