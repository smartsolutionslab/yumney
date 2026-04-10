using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class IngredientCategoryResolverTests
{
    [Theory]
    [InlineData("onion", "produce")]
    [InlineData("tomato", "produce")]
    [InlineData("apple", "produce")]
    [InlineData("spinach", "produce")]
    public void Resolve_EnglishProduce_ReturnsProduce(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("zwiebel", "produce")]
    [InlineData("kartoffel", "produce")]
    [InlineData("knoblauch", "produce")]
    public void Resolve_GermanProduce_ReturnsProduce(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("milk", "dairy")]
    [InlineData("cheese", "dairy")]
    [InlineData("egg", "dairy")]
    [InlineData("milch", "dairy")]
    [InlineData("käse", "dairy")]
    [InlineData("eier", "dairy")]
    public void Resolve_Dairy_ReturnsDairy(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("chicken", "meat-fish")]
    [InlineData("salmon", "meat-fish")]
    [InlineData("hähnchen", "meat-fish")]
    [InlineData("lachs", "meat-fish")]
    [InlineData("hackfleisch", "meat-fish")]
    public void Resolve_MeatFish_ReturnsMeatFish(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("bread", "bakery")]
    [InlineData("brot", "bakery")]
    [InlineData("croissant", "bakery")]
    public void Resolve_Bakery_ReturnsBakery(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("juice", "beverages")]
    [InlineData("coffee", "beverages")]
    [InlineData("kaffee", "beverages")]
    [InlineData("bier", "beverages")]
    public void Resolve_Beverages_ReturnsBeverages(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("flour", "pantry")]
    [InlineData("rice", "pantry")]
    [InlineData("pasta", "pantry")]
    [InlineData("mehl", "pantry")]
    [InlineData("reis", "pantry")]
    [InlineData("nudeln", "pantry")]
    public void Resolve_Pantry_ReturnsPantry(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("toilet paper", "household")]
    [InlineData("dish soap", "household")]
    [InlineData("toilettenpapier", "household")]
    [InlineData("spülmittel", "household")]
    public void Resolve_Household_ReturnsHousehold(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("ice cream", "frozen")]
    [InlineData("fish sticks", "frozen")]
    public void Resolve_Frozen_ReturnsFrozen(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("onions", "produce")]
    [InlineData("carrots", "produce")]
    [InlineData("lemons", "produce")]
    public void Resolve_EnglishPlural_NormalizesToSingular(string item, string expected)
    {
        var result = IngredientCategoryResolver.Resolve(item);

        result.Should().NotBeNull();
        result!.Value.Should().Be(expected);
    }

    [Fact]
    public void Resolve_UnknownItem_ReturnsNull()
    {
        var result = IngredientCategoryResolver.Resolve("tahini");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyInput_ReturnsNull(string? item)
    {
        var result = IngredientCategoryResolver.Resolve(item!);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_CaseInsensitive_MatchesItem()
    {
        var result = IngredientCategoryResolver.Resolve("MILK");

        result.Should().NotBeNull();
        result!.Value.Should().Be("dairy");
    }

    [Fact]
    public void Resolve_TrimmedInput_MatchesItem()
    {
        var result = IngredientCategoryResolver.Resolve("  chicken  ");

        result.Should().NotBeNull();
        result!.Value.Should().Be("meat-fish");
    }
}
