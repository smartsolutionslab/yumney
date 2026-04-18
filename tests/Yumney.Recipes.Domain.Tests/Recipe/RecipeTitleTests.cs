using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeTitleTests
{
	[Fact]
	public void Constructor_ValidTitle_CreatesInstance()
	{
		var title = RecipeTitle.From("Pasta Carbonara");

		title.Value.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public void Constructor_TrimsWhitespace()
	{
		var title = RecipeTitle.From("  Pasta Carbonara  ");

		title.Value.Should().Be("Pasta Carbonara");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => RecipeTitle.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_ExceedsMaxLength_ThrowsGuardException()
	{
		var value = new string('a', 201);

		var act = () => RecipeTitle.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_AtMaxLength_CreatesInstance()
	{
		var value = new string('a', 200);

		var title = RecipeTitle.From(value);

		title.Value.Should().HaveLength(200);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var title1 = RecipeTitle.From("Pasta");
		var title2 = RecipeTitle.From("Pasta");

		title1.Should().Be(title2);
	}
}
