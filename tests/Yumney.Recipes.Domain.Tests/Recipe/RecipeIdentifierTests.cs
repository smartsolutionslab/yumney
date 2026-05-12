using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeIdentifierTests
{
	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();

		var identifier = RecipeIdentifier.From(guid);

		identifier.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => RecipeIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void New_GeneratesUniqueId()
	{
		var first = RecipeIdentifier.New();
		var second = RecipeIdentifier.New();

		first.Should().NotBe(second);
	}

	[Fact]
	public void New_IsNotEmpty()
	{
		var identifier = RecipeIdentifier.New();

		identifier.Value.Should().NotBeEmpty();
	}

	[Fact]
	public void FromNullable_WithValue_ReturnsInstance()
	{
		var guid = Guid.NewGuid();

		var identifier = RecipeIdentifier.FromNullable(guid);

		identifier.Should().NotBeNull();
		identifier!.Value.Should().Be(guid);
	}

	[Fact]
	public void FromNullable_WithNullGuid_ReturnsNull()
	{
		var identifier = RecipeIdentifier.FromNullable((Guid?)null);

		identifier.Should().BeNull();
	}

	[Fact]
	public void FromNullable_WithNullString_ReturnsNull()
	{
		var identifier = RecipeIdentifier.FromNullable((string?)null);

		identifier.Should().BeNull();
	}

	[Fact]
	public void FromNullable_WithValidGuidString_ReturnsInstance()
	{
		var guid = Guid.NewGuid();

		var identifier = RecipeIdentifier.FromNullable(guid.ToString());

		identifier.Should().NotBeNull();
		identifier!.Value.Should().Be(guid);
	}

	[Fact]
	public void FromNullable_WithInvalidString_ReturnsNull()
	{
		var identifier = RecipeIdentifier.FromNullable("not-a-guid");

		identifier.Should().BeNull();
	}

	[Fact]
	public void ToString_ReturnsGuidString()
	{
		var guid = Guid.NewGuid();

		var identifier = RecipeIdentifier.From(guid);

		identifier.ToString().Should().Be(guid.ToString());
	}

	[Fact]
	public void Equality_SameGuid_AreEqual()
	{
		var guid = Guid.NewGuid();

		var first = RecipeIdentifier.From(guid);
		var second = RecipeIdentifier.From(guid);

		first.Should().Be(second);
	}
}
