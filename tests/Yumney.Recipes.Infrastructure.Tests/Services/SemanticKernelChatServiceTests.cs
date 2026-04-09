using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class SemanticKernelChatServiceTests
{
    [Fact]
    public void MatchRecipesByMention_ExactTitleInReply_ReturnsSuggestion()
    {
        var recipes = new List<Recipe> { MakeRecipe("Pasta Carbonara") };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "You could try Pasta Carbonara tonight!", recipes);

        result.Should().ContainSingle(s => s.Title == "Pasta Carbonara");
    }

    [Fact]
    public void MatchRecipesByMention_CaseInsensitive_ReturnsSuggestion()
    {
        var recipes = new List<Recipe> { MakeRecipe("Classic Lasagne") };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "I recommend classic lasagne for dinner.", recipes);

        result.Should().ContainSingle(s => s.Title == "Classic Lasagne");
    }

    [Fact]
    public void MatchRecipesByMention_PartialWordMatch_DoesNotReturn()
    {
        var recipes = new List<Recipe> { MakeRecipe("Chicken Stir Fry") };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "Try chickpea curry instead.", recipes);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MatchRecipesByMention_TitleNotInReply_ReturnsEmpty()
    {
        var recipes = new List<Recipe> { MakeRecipe("Beef Wellington") };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "How about a nice salad?", recipes);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MatchRecipesByMention_MultipleMatches_ReturnsAll()
    {
        var recipes = new List<Recipe>
        {
            MakeRecipe("Tomato Soup"),
            MakeRecipe("Grilled Cheese"),
        };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "Tomato Soup pairs great with Grilled Cheese.", recipes);

        result.Should().HaveCount(2);
        result.Select(s => s.Title).Should().Contain("Tomato Soup").And.Contain("Grilled Cheese");
    }

    [Fact]
    public void MatchRecipesByMention_TitleWithSpecialChars_MatchesSafely()
    {
        var recipes = new List<Recipe> { MakeRecipe("Mac & Cheese (Easy)") };

        var result = SemanticKernelChatService.MatchRecipesByMention(
            "Try Mac & Cheese (Easy) for a quick meal.", recipes);

        result.Should().ContainSingle(s => s.Title == "Mac & Cheese (Easy)");
    }

    [Fact]
    public void MatchRecipesByMention_EmptyReply_ReturnsEmpty()
    {
        var recipes = new List<Recipe> { MakeRecipe("Pasta") };

        var result = SemanticKernelChatService.MatchRecipesByMention(string.Empty, recipes);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MatchRecipesByMention_EmptyRecipeList_ReturnsEmpty()
    {
        var result = SemanticKernelChatService.MatchRecipesByMention(
            "What can I cook?", []);

        result.Should().BeEmpty();
    }

    private static Recipe MakeRecipe(string title)
    {
        return Recipe.Create(
            RecipeTitle.From(title),
            OwnerIdentifier.From("owner-1"),
            [Ingredient.Create(IngredientName.From("Salt"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Cook"))]);
    }
}
