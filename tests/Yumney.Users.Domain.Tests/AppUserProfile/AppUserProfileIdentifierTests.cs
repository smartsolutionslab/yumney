using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class AppUserProfileIdentifierTests
{
    [Fact]
    public void Constructor_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();

        var identifier = AppUserProfileIdentifier.From(guid);

        identifier.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsGuardException()
    {
        var act = () => AppUserProfileIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();

        var identifier = AppUserProfileIdentifier.From(guid);

        identifier.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();

        var a = AppUserProfileIdentifier.From(guid);
        var b = AppUserProfileIdentifier.From(guid);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = AppUserProfileIdentifier.New();
        var b = AppUserProfileIdentifier.New();

        a.Should().NotBe(b);
    }
}
