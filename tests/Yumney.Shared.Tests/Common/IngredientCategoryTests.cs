using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class IngredientCategoryTests
{
    [Theory]
    [InlineData("produce")]
    [InlineData("dairy")]
    [InlineData("meat-fish")]
    [InlineData("bakery")]
    [InlineData("frozen")]
    [InlineData("beverages")]
    [InlineData("pantry")]
    [InlineData("household")]
    [InlineData("other")]
    public void From_ValidValue_CreatesInstance(string value)
    {
        var category = IngredientCategory.From(value);

        category.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("Produce")]
    [InlineData("DAIRY")]
    [InlineData("invalid")]
    public void From_InvalidValue_ThrowsGuardException(string value)
    {
        var act = () => IngredientCategory.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void All_Contains9Categories()
    {
        IngredientCategory.All.Should().HaveCount(9);
    }

    [Fact]
    public void DisplayOrder_ProduceIsFirst()
    {
        IngredientCategory.Produce.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void DisplayOrder_OtherIsLast()
    {
        IngredientCategory.Other.DisplayOrder.Should().Be(8);
    }

    [Fact]
    public void DisplayOrder_IsConsecutive()
    {
        for (var i = 0; i < IngredientCategory.All.Count; i++)
        {
            IngredientCategory.All[i].DisplayOrder.Should().Be(i);
        }
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var c1 = IngredientCategory.From("dairy");
        var c2 = IngredientCategory.From("dairy");

        c1.Should().Be(c2);
    }
}
