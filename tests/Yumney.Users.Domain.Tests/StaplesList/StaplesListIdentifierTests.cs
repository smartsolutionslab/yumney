using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.StaplesList;

public class StaplesListIdentifierTests
{
	[Fact]
	public void New_CreatesNonEmptyGuid()
	{
		var id = StaplesListIdentifier.New();

		id.Value.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();
		var id = StaplesListIdentifier.From(guid);

		id.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => StaplesListIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}
}
