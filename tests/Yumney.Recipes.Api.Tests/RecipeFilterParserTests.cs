using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Api;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests;

public class RecipeFilterParserTests
{
    [Fact]
    public void Build_AllNull_ReturnsNull()
    {
        var result = RecipeFilterParser.Build(null, null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Build_EmptyTags_ReturnsNull()
    {
        var result = RecipeFilterParser.Build(string.Empty, null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Build_WhitespaceTags_ReturnsNull()
    {
        var result = RecipeFilterParser.Build("  ", null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Build_SingleTag_ReturnsFilterWithOneTag()
    {
        var result = RecipeFilterParser.Build("vegan", null, null, null);

        result.Should().NotBeNull();
        result!.Tags.Should().ContainSingle(t => t.Value == "vegan");
    }

    [Fact]
    public void Build_MultipleTags_SplitsByComma()
    {
        var result = RecipeFilterParser.Build("vegan,italian,quick", null, null, null);

        result!.Tags.Should().HaveCount(3);
        result.Tags!.Select(t => t.Value).Should().Contain("vegan").And.Contain("italian").And.Contain("quick");
    }

    [Fact]
    public void Build_TagsWithSpaces_TrimsEachTag()
    {
        var result = RecipeFilterParser.Build(" vegan , italian ", null, null, null);

        result!.Tags!.Select(t => t.Value).Should().Contain("vegan").And.Contain("italian");
    }

    [Fact]
    public void Build_TagsWithEmptySegments_IgnoresEmpty()
    {
        var result = RecipeFilterParser.Build("vegan,,italian,", null, null, null);

        result!.Tags.Should().HaveCount(2);
    }

    [Fact]
    public void Build_WithDifficulty_ReturnsDifficultyVo()
    {
        var result = RecipeFilterParser.Build(null, "easy", null, null);

        result.Should().NotBeNull();
        result!.Difficulty!.Value.Should().Be("Easy");
    }

    [Fact]
    public void Build_WithMaxPrepTime_ReturnsPreparationTime()
    {
        var result = RecipeFilterParser.Build(null, null, 30, null);

        result.Should().NotBeNull();
        result!.MaxPrepTime!.Value.Should().Be(30);
    }

    [Fact]
    public void Build_WithMaxCookTime_ReturnsCookingTime()
    {
        var result = RecipeFilterParser.Build(null, null, null, 45);

        result.Should().NotBeNull();
        result!.MaxCookTime!.Value.Should().Be(45);
    }

    [Fact]
    public void Build_WithFavoritesOnly_ReturnsFavoritesFlag()
    {
        var result = RecipeFilterParser.Build(null, null, null, null, true);

        result.Should().NotBeNull();
        result!.FavoritesOnly.Should().BeTrue();
    }

    [Fact]
    public void Build_FavoritesOnlyFalse_ReturnsNull()
    {
        var result = RecipeFilterParser.Build(null, null, null, null, false);

        result.Should().BeNull();
    }

    [Fact]
    public void Build_AllFieldsPopulated_ReturnsCompleteFilter()
    {
        var result = RecipeFilterParser.Build("vegan", "medium", 20, 40, true);

        result.Should().NotBeNull();
        result!.Tags.Should().ContainSingle(t => t.Value == "vegan");
        result.Difficulty!.Value.Should().Be("Medium");
        result.MaxPrepTime!.Value.Should().Be(20);
        result.MaxCookTime!.Value.Should().Be(40);
        result.FavoritesOnly.Should().BeTrue();
    }
}
