using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class DefaultServingsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(12)]
    public void From_ValidValue_CreatesInstance(int value)
    {
        var servings = DefaultServings.From(value);

        servings.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(100)]
    public void From_OutOfRange_ThrowsGuardException(int value)
    {
        var act = () => DefaultServings.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Default_StaticInstance_HasValue4()
    {
        DefaultServings.Default.Value.Should().Be(4);
    }

    [Fact]
    public void FromNullable_WithValue_CreatesInstance()
    {
        var servings = DefaultServings.FromNullable(4);

        servings.Should().NotBeNull();
        servings!.Value.Should().Be(4);
    }

    [Fact]
    public void FromNullable_WithNull_ReturnsNull()
    {
        var servings = DefaultServings.FromNullable(null);

        servings.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        var servings = DefaultServings.From(6);

        int value = servings;

        value.Should().Be(6);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var s1 = DefaultServings.From(4);
        var s2 = DefaultServings.From(4);

        s1.Should().Be(s2);
    }

    [Fact]
    public void ToString_ReturnsValueAsString()
    {
        var servings = DefaultServings.From(6);

        servings.ToString().Should().Be("6");
    }
}
