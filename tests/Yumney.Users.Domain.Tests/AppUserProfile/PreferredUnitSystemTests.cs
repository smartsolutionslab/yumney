using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class PreferredUnitSystemTests
{
    [Theory]
    [InlineData("metric")]
    [InlineData("imperial")]
    public void Constructor_ValidValue_CreatesInstance(string value)
    {
        var unitSystem = new PreferredUnitSystem(value);

        unitSystem.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new PreferredUnitSystem(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var unitSystem = new PreferredUnitSystem(new string('a', 20));

        unitSystem.Value.Should().HaveLength(20);
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => new PreferredUnitSystem(new string('a', 21));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var unitSystem = new PreferredUnitSystem("metric");

        unitSystem.ToString().Should().Be("metric");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var us1 = new PreferredUnitSystem("metric");
        var us2 = new PreferredUnitSystem("metric");

        us1.Should().Be(us2);
    }
}
