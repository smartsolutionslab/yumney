using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class DisplayNameTests
{
    [Fact]
    public void Constructor_ValidName_CreatesInstance()
    {
        var name = DisplayName.From("Test User");

        name.Value.Should().Be("Test User");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var name = DisplayName.From("  Test User  ");

        name.Value.Should().Be("Test User");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => DisplayName.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => DisplayName.From(new string('A', 201));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var name = DisplayName.From(new string('A', 200));

        name.Value.Should().HaveLength(200);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var name1 = DisplayName.From("Test User");
        var name2 = DisplayName.From("Test User");

        name1.Should().Be(name2);
    }
}
