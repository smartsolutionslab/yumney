using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class DietaryRestrictionTests
{
	[Theory]
	[InlineData("gluten-free")]
	[InlineData("lactose-free")]
	[InlineData("nut-allergy")]
	[InlineData("egg-free")]
	[InlineData("soy-free")]
	[InlineData("shellfish-allergy")]
	[InlineData("halal")]
	[InlineData("kosher")]
	public void From_ValidValue_CreatesInstance(string value)
	{
		var restriction = DietaryRestriction.From(value);

		restriction.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => DietaryRestriction.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData("Gluten-Free")]
	[InlineData("sugar-free")]
	[InlineData("dairy-free")]
	public void From_UnsupportedValue_ThrowsGuardException(string value)
	{
		var act = () => DietaryRestriction.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void AllowedValues_ContainsAllStaticInstances()
	{
		DietaryRestriction.AllowedValues.Should().HaveCount(8);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var r1 = DietaryRestriction.From("gluten-free");
		var r2 = DietaryRestriction.From("gluten-free");

		r1.Should().Be(r2);
	}
}
