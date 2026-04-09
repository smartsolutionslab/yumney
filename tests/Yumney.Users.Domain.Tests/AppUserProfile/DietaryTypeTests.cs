using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class DietaryTypeTests
{
    [Theory]
    [InlineData("omnivore")]
    [InlineData("vegetarian")]
    [InlineData("vegan")]
    [InlineData("pescatarian")]
    [InlineData("flexitarian")]
    public void From_ValidValue_CreatesInstance(string value)
    {
        var type = DietaryType.From(value);

        type.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => DietaryType.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData("Vegan")]
    [InlineData("OMNIVORE")]
    [InlineData("keto")]
    [InlineData("paleo")]
    public void From_UnsupportedValue_ThrowsGuardException(string value)
    {
        var act = () => DietaryType.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void StaticInstances_HaveCorrectValues()
    {
        DietaryType.Omnivore.Value.Should().Be("omnivore");
        DietaryType.Vegetarian.Value.Should().Be("vegetarian");
        DietaryType.Vegan.Value.Should().Be("vegan");
        DietaryType.Pescatarian.Value.Should().Be("pescatarian");
        DietaryType.Flexitarian.Value.Should().Be("flexitarian");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var t1 = DietaryType.From("vegan");
        var t2 = DietaryType.From("vegan");

        t1.Should().Be(t2);
    }
}
