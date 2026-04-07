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
    public void Constructor_SupportedValue_CreatesInstance(string value)
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

    [Theory]
    [InlineData("Metric")]
    [InlineData("METRIC")]
    [InlineData("us")]
    [InlineData("uk")]
    public void Constructor_UnsupportedValue_ThrowsGuardException(string value)
    {
        var act = () => PreferredUnitSystem.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Metric_StaticInstance_HasValueMetric()
    {
        PreferredUnitSystem.Metric.Value.Should().Be("metric");
    }

    [Fact]
    public void Imperial_StaticInstance_HasValueImperial()
    {
        PreferredUnitSystem.Imperial.Value.Should().Be("imperial");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var us1 = PreferredUnitSystem.From("metric");
        var us2 = PreferredUnitSystem.From("metric");

        us1.Should().Be(us2);
    }
}
