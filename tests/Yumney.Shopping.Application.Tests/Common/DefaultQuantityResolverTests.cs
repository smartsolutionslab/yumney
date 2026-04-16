using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Application.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Common;

public class DefaultQuantityResolverTests
{
    [Theory]
    [InlineData("milk", 1, "L")]
    [InlineData("cream", 1, "L")]
    [InlineData("juice", 1, "L")]
    [InlineData("oil", 0.5, "L")]
    public void Resolve_EnglishLiquid_ReturnsLiters(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData("eggs", 6)]
    [InlineData("egg", 6)]
    public void Resolve_EnglishEggs_Returns6Pieces(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("pc");
    }

    [Theory]
    [InlineData("butter", 250)]
    [InlineData("cheese", 250)]
    public void Resolve_EnglishDairy_Returns250Grams(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("g");
    }

    [Theory]
    [InlineData("chicken", 500)]
    [InlineData("beef", 500)]
    [InlineData("salmon", 500)]
    public void Resolve_EnglishMeatFish_Returns500Grams(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("g");
    }

    [Theory]
    [InlineData("onion", 1, "pc")]
    [InlineData("potato", 1, "pc")]
    [InlineData("tomato", 1, "pc")]
    [InlineData("carrot", 1, "pc")]
    public void Resolve_EnglishVegetable_Returns1Piece(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData("milch", 1, "L")]
    [InlineData("sahne", 1, "L")]
    [InlineData("saft", 1, "L")]
    [InlineData("wasser", 1, "L")]
    [InlineData("olivenöl", 0.5, "L")]
    public void Resolve_GermanLiquid_ReturnsLiters(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData("ei", 6)]
    [InlineData("eier", 6)]
    public void Resolve_GermanEggs_Returns6Pieces(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("pc");
    }

    [Theory]
    [InlineData("käse", 250)]
    [InlineData("kaese", 250)]
    public void Resolve_GermanDairy_Returns250Grams(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("g");
    }

    [Theory]
    [InlineData("hähnchen", 500)]
    [InlineData("rindfleisch", 500)]
    [InlineData("hackfleisch", 500)]
    [InlineData("lachs", 500)]
    [InlineData("fisch", 500)]
    public void Resolve_GermanMeatFish_Returns500Grams(string item, decimal amount)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be("g");
    }

    [Theory]
    [InlineData("zwiebel", 1, "pc")]
    [InlineData("kartoffel", 1, "pc")]
    [InlineData("tomate", 1, "pc")]
    [InlineData("karotte", 1, "pc")]
    [InlineData("knoblauch", 1, "pc")]
    public void Resolve_GermanVegetable_Returns1Piece(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData("mehl", 1, "kg")]
    [InlineData("zucker", 1, "kg")]
    [InlineData("reis", 1, "kg")]
    [InlineData("nudeln", 500, "g")]
    public void Resolve_GermanStaples_ReturnsCorrectAmount(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData("onions", 1, "pc")]
    [InlineData("potatoes", 1, "pc")]
    [InlineData("tomatoes", 1, "pc")]
    [InlineData("carrots", 1, "pc")]
    [InlineData("lemons", 1, "pc")]
    public void Resolve_EnglishPlural_NormalizesToSingular(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Fact]
    public void Resolve_UnknownItem_ReturnsFallback()
    {
        var result = DefaultQuantityResolver.Resolve("toilet paper");

        result.Should().Be(DefaultQuantityResolver.OnePiece);
    }

    [Fact]
    public void Resolve_UnknownItemWithCategory_UsesCategoryDefault()
    {
        var result = DefaultQuantityResolver.Resolve("coconut milk", "liquid");

        result.Amount.Value.Should().Be(1);
        result.Unit!.Value.Should().Be("L");
    }

    [Fact]
    public void Resolve_UnknownItemWithUnknownCategory_ReturnsFallback()
    {
        var result = DefaultQuantityResolver.Resolve("mystery item", "unknown-category");

        result.Should().Be(DefaultQuantityResolver.OnePiece);
    }

    [Fact]
    public void Resolve_CaseInsensitive_MatchesItem()
    {
        var result = DefaultQuantityResolver.Resolve("MILK");

        result.Amount.Value.Should().Be(1);
        result.Unit!.Value.Should().Be("L");
    }

    [Theory]
    [InlineData("  milk  ", 1, "L")]
    [InlineData("  chicken  ", 500, "g")]
    public void Resolve_TrimmedInput_MatchesItem(string item, decimal amount, string unit)
    {
        var result = DefaultQuantityResolver.Resolve(item);

        result.Amount.Value.Should().Be(amount);
        result.Unit!.Value.Should().Be(unit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyInput_ReturnsFallback(string? item)
    {
        var result = DefaultQuantityResolver.Resolve(item!);

        result.Should().Be(DefaultQuantityResolver.OnePiece);
    }

    [Fact]
    public void Resolve_Pasta_Returns500Grams()
    {
        var result = DefaultQuantityResolver.Resolve("pasta");

        result.Amount.Value.Should().Be(500);
        result.Unit!.Value.Should().Be("g");
    }

    [Fact]
    public void Resolve_Flour_Returns1Kg()
    {
        var result = DefaultQuantityResolver.Resolve("flour");

        result.Amount.Value.Should().Be(1);
        result.Unit!.Value.Should().Be("kg");
    }

    [Fact]
    public void Resolve_PluralDoesNotBreakDirectMatch()
    {
        // "eggs" is a direct match, not a plural normalization
        var result = DefaultQuantityResolver.Resolve("eggs");

        result.Amount.Value.Should().Be(6);
    }
}
