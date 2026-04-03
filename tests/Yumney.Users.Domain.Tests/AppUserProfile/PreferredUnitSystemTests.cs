using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class PreferredUnitSystemTests
{
    [Theory]
    [InlineData("metric")]
    [InlineData("imperial")]
    public void Constructor_ValidValue_CreatesInstance(string value)
    {
        var unitSystem = PreferredUnitSystem.From(value);

        unitSystem.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => PreferredUnitSystem.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var unitSystem = PreferredUnitSystem.From(new string('a', 20));

        unitSystem.Value.Should().HaveLength(20);
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => PreferredUnitSystem.From(new string('a', 21));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var us1 = PreferredUnitSystem.From("metric");
        var us2 = PreferredUnitSystem.From("metric");

        us1.Should().Be(us2);
    }
}
