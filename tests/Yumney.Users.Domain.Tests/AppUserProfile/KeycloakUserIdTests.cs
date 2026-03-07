using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class KeycloakUserIdTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var id = new KeycloakUserId("abc-123");

        id.Value.Should().Be("abc-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new KeycloakUserId(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = new KeycloakUserId("abc-123");

        id.ToString().Should().Be("abc-123");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = new KeycloakUserId("abc-123");
        var id2 = new KeycloakUserId("abc-123");

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = new KeycloakUserId("abc-123");
        var id2 = new KeycloakUserId("def-456");

        id1.Should().NotBe(id2);
    }
}
