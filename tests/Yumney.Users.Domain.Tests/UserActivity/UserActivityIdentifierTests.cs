using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class UserActivityIdentifierTests
{
    [Fact]
    public void New_CreatesNonEmptyGuid()
    {
        var id = UserActivityIdentifier.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();
        var id = UserActivityIdentifier.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsGuardException()
    {
        var act = () => UserActivityIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = UserActivityIdentifier.From(guid);
        var b = UserActivityIdentifier.From(guid);

        a.Should().Be(b);
    }
}
