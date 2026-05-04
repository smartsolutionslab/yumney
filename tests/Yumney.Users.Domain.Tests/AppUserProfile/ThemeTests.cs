using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class ThemeTests
{
	[Theory]
	[InlineData("light")]
	[InlineData("dark")]
	[InlineData("system")]
	public void From_ValidValue_CreatesInstance(string value)
	{
		Theme.From(value).Value.Should().Be(value);
	}

	[Theory]
	[InlineData("solarized")]
	[InlineData("Light")]
	[InlineData("DARK")]
	[InlineData("")]
	public void From_UnsupportedOrInvalidValue_ThrowsGuardException(string value)
	{
		var act = () => Theme.From(value);
		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void StaticInstances_HaveCorrectValues()
	{
		Theme.Light.Value.Should().Be("light");
		Theme.Dark.Value.Should().Be("dark");
		Theme.System.Value.Should().Be("system");
	}
}
