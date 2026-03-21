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

        var identifier = new AppUserProfileIdentifier(guid);

        identifier.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsGuardException()
    {
        var act = () => new AppUserProfileIdentifier(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();

        var identifier = new AppUserProfileIdentifier(guid);

        identifier.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();

        var a = new AppUserProfileIdentifier(guid);
        var b = new AppUserProfileIdentifier(guid);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new AppUserProfileIdentifier(Guid.NewGuid());
        var b = new AppUserProfileIdentifier(Guid.NewGuid());

        a.Should().NotBe(b);
    }
}
