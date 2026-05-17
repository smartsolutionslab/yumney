using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Infrastructure;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests;

public class UsersDiagnosticsTests
{
	[Fact]
	public void SourceName_IsTheCanonicalYumneyUsersConstant()
	{
		UsersDiagnostics.SourceName.Should().Be("Yumney.Users");
	}

	[Fact]
	public void ActivitySource_NameMatchesSourceName()
	{
		UsersDiagnostics.ActivitySource.Name.Should().Be(UsersDiagnostics.SourceName);
	}
}
