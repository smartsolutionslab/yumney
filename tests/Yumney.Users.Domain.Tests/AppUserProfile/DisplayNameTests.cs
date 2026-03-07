using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class DisplayNameTests
{
    [Fact]
    public void Constructor_ValidName_CreatesInstance()
    {
        var name = new DisplayName("Test User");

        name.Value.Should().Be("Test User");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var name = new DisplayName("  Test User  ");

        name.Value.Should().Be("Test User");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new DisplayName(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => new DisplayName(new string('A', 201));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var name = new DisplayName(new string('A', 200));

        name.Value.Should().HaveLength(200);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new DisplayName("Test User");

        name.ToString().Should().Be("Test User");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var name1 = new DisplayName("Test User");
        var name2 = new DisplayName("Test User");

        name1.Should().Be(name2);
    }
}
