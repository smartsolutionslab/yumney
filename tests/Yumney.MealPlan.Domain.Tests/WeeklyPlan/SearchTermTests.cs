using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class SearchTermTests
{
	[Fact]
	public void From_TrimsSurroundingWhitespace()
	{
		var term = SearchTerm.From("  carbonara  ");

		term.Value.Should().Be("carbonara");
	}

	[Fact]
	public void From_AtMaxLength_IsAccepted()
	{
		var term = SearchTerm.From(new string('x', SearchTerm.MaxLength));

		term.Value.Length.Should().Be(SearchTerm.MaxLength);
	}

	[Fact]
	public void From_BeyondMaxLength_Throws()
	{
		var act = () => SearchTerm.From(new string('x', SearchTerm.MaxLength + 1));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_EmptyString_Throws()
	{
		var act = () => SearchTerm.From(string.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Whitespace_Throws()
	{
		var act = () => SearchTerm.From("   ");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_Null_ReturnsNull()
	{
		SearchTerm.FromNullable(null).Should().BeNull();
	}

	[Fact]
	public void FromNullable_Empty_ReturnsNull()
	{
		// HasValue() treats null + whitespace-only as "no value" — the search
		// is optional; a blank input shouldn't synthesise a guard exception.
		SearchTerm.FromNullable(string.Empty).Should().BeNull();
	}

	[Fact]
	public void FromNullable_Whitespace_ReturnsNull()
	{
		SearchTerm.FromNullable("   ").Should().BeNull();
	}

	[Fact]
	public void FromNullable_NonEmpty_ReturnsTrimmedTerm()
	{
		var term = SearchTerm.FromNullable("  pasta  ");

		term.Should().NotBeNull();
		term!.Value.Should().Be("pasta");
	}

	[Fact]
	public void ImplicitConversion_ToString_YieldsValue()
	{
		var term = SearchTerm.From("risotto");
		string raw = term;

		raw.Should().Be("risotto");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = SearchTerm.From("pasta");
		var b = SearchTerm.From("pasta");

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
