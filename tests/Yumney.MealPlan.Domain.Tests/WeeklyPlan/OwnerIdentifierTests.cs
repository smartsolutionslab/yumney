using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class OwnerIdentifierTests
{
	[Fact]
	public void From_ValidValue_CreatesInstance()
	{
		var owner = OwnerIdentifier.From("user-123");

		owner.Value.Should().Be("user-123");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => OwnerIdentifier.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_ExceedsMaxLength_ThrowsGuardException()
	{
		var longValue = new string('a', OwnerIdentifier.MaxLength + 1);

		var act = () => OwnerIdentifier.From(longValue);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_TrimsWhitespace()
	{
		var owner = OwnerIdentifier.From("  user-123  ");

		owner.Value.Should().Be("user-123");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = OwnerIdentifier.From("user-123");
		var b = OwnerIdentifier.From("user-123");

		a.Should().Be(b);
	}

	[Fact]
	public void ImplicitConversion_ReturnsValue()
	{
		string value = OwnerIdentifier.From("user-123");

		value.Should().Be("user-123");
	}
}
