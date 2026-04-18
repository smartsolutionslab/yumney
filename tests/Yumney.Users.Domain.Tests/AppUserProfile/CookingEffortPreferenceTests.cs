using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class CookingEffortPreferenceTests
{
	[Theory]
	[InlineData("quick-weekdays")]
	[InlineData("balanced")]
	[InlineData("elaborate-weekends")]
	public void From_ValidValue_CreatesInstance(string value)
	{
		var pref = CookingEffortPreference.From(value);

		pref.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => CookingEffortPreference.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData("Quick-Weekdays")]
	[InlineData("lazy")]
	[InlineData("always-elaborate")]
	public void From_UnsupportedValue_ThrowsGuardException(string value)
	{
		var act = () => CookingEffortPreference.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void StaticInstances_HaveCorrectValues()
	{
		CookingEffortPreference.QuickWeekdays.Value.Should().Be("quick-weekdays");
		CookingEffortPreference.Balanced.Value.Should().Be("balanced");
		CookingEffortPreference.ElaborateWeekends.Value.Should().Be("elaborate-weekends");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var p1 = CookingEffortPreference.From("balanced");
		var p2 = CookingEffortPreference.From("balanced");

		p1.Should().Be(p2);
	}
}
