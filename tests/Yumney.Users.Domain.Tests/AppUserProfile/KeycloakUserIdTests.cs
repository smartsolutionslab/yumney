using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class KeycloakUserIdTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var id = KeycloakUserId.From("abc-123");

        id.Value.Should().Be("abc-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => KeycloakUserId.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = KeycloakUserId.From("abc-123");
        var id2 = KeycloakUserId.From("abc-123");

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = KeycloakUserId.From("abc-123");
        var id2 = KeycloakUserId.From("def-456");

        id1.Should().NotBe(id2);
    }
}
