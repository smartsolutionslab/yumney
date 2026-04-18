using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeUrlTests
{
	[Theory]
	[InlineData("http://example.com/recipe")]
	[InlineData("http://www.example.com/recipe/123")]
	public void Constructor_ValidHttpUrl_CreatesInstance(string value)
	{
		var url = RecipeUrl.From(value);

		url.Value.Should().Be(value);
	}

	[Theory]
	[InlineData("https://example.com/recipe")]
	[InlineData("https://www.example.com/recipe/123")]
	public void Constructor_ValidHttpsUrl_CreatesInstance(string value)
	{
		var url = RecipeUrl.From(value);

		url.Value.Should().Be(value);
	}

	[Fact]
	public void Constructor_UrlWithQueryParams_CreatesInstance()
	{
		var value = "https://example.com/recipe?id=123&lang=en";

		var url = RecipeUrl.From(value);

		url.Value.Should().Be(value);
	}

	[Fact]
	public void Constructor_UrlWithFragment_CreatesInstance()
	{
		var value = "https://example.com/recipe#ingredients";

		var url = RecipeUrl.From(value);

		url.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => RecipeUrl.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData("ftp://example.com/recipe")]
	[InlineData("example.com/recipe")]
	[InlineData("not-a-url")]
	[InlineData("javascript:alert(1)")]
	public void Constructor_InvalidFormat_ThrowsGuardException(string value)
	{
		var act = () => RecipeUrl.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_ExceedsMaxLength_ThrowsGuardException()
	{
		var path = new string('a', 2040);
		var url = $"https://x.com/{path}";

		var act = () => RecipeUrl.From(url);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_AtMaxLength_CreatesInstance()
	{
		var prefix = "https://x.com/";
		var path = new string('a', 2048 - prefix.Length);
		var url = $"{prefix}{path}";

		var result = RecipeUrl.From(url);

		result.Value.Should().HaveLength(2048);
	}

	[Fact]
	public void Constructor_TrimsWhitespace()
	{
		var url = RecipeUrl.From("  https://example.com/recipe  ");

		url.Value.Should().Be("https://example.com/recipe");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var url1 = RecipeUrl.From("https://example.com/recipe");
		var url2 = RecipeUrl.From("https://example.com/recipe");

		url1.Should().Be(url2);
	}

	[Fact]
	public void Equality_DifferentValue_AreNotEqual()
	{
		var url1 = RecipeUrl.From("https://example.com/recipe1");
		var url2 = RecipeUrl.From("https://example.com/recipe2");

		url1.Should().NotBe(url2);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void FromNullable_NullOrWhitespace_ReturnsNull(string? value)
	{
		var result = RecipeUrl.FromNullable(value);

		result.Should().BeNull();
	}

	[Fact]
	public void FromNullable_ValidUrl_ReturnsRecipeUrl()
	{
		var result = RecipeUrl.FromNullable("https://example.com/recipe");

		result.Should().NotBeNull();
		result!.Value.Should().Be("https://example.com/recipe");
	}
}
