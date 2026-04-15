using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class ItemSourceTests
{
    [Fact]
    public void From_ValidValue_CreatesInstance()
    {
        var source = ItemSource.From("recipe:abc");

        source.Value.Should().Be("recipe:abc");
    }

    [Fact]
    public void From_Trims_WhiteSpace()
    {
        var source = ItemSource.From("  manual  ");

        source.Value.Should().Be("manual");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_EmptyValue_ThrowsGuardException(string? value)
    {
        var act = () => ItemSource.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_TooLong_ThrowsGuardException()
    {
        var act = () => ItemSource.From(new string('a', 101));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Manual_ReturnsManualSource()
    {
        var source = ItemSource.Manual;

        source.Value.Should().Be("manual");
    }

    [Fact]
    public void MealPlan_ReturnsMealPlanSource()
    {
        var source = ItemSource.MealPlan;

        source.Value.Should().Be("meal-plan");
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        string result = ItemSource.From("manual");

        result.Should().Be("manual");
    }
}
