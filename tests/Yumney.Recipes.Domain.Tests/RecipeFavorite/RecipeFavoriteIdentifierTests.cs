using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.RecipeFavorite;

public class RecipeFavoriteIdentifierTests
{
	[Fact]
	public void New_ProducesNonEmptyGuid()
	{
		var id = RecipeFavoriteIdentifier.New();

		id.Value.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public void New_TwoCalls_ProduceDistinctIdentifiers()
	{
		var first = RecipeFavoriteIdentifier.New();
		var second = RecipeFavoriteIdentifier.New();

		first.Should().NotBe(second);
	}

	[Fact]
	public void From_NonEmptyGuid_RoundTrips()
	{
		var guid = Guid.NewGuid();

		var id = RecipeFavoriteIdentifier.From(guid);

		id.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_Throws()
	{
		var act = () => RecipeFavoriteIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ToString_ReturnsGuidString()
	{
		var guid = Guid.NewGuid();
		var id = RecipeFavoriteIdentifier.From(guid);

		id.ToString().Should().Be(guid.ToString());
	}

	[Fact]
	public void Equality_SameUnderlyingGuid_AreEqual()
	{
		var guid = Guid.NewGuid();

		var a = RecipeFavoriteIdentifier.From(guid);
		var b = RecipeFavoriteIdentifier.From(guid);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
