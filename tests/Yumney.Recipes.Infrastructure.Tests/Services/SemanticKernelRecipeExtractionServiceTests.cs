using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1311
public class SemanticKernelRecipeExtractionServiceTests
{
    private static readonly string validRecipeJson = """
        {
          "title": "Pasta Carbonara",
          "description": "A classic Italian dish",
          "ingredients": [
            { "name": "Spaghetti", "amount": 400, "unit": "g" },
            { "name": "Eggs", "amount": 3, "unit": null }
          ],
          "steps": [
            { "number": 1, "description": "Cook pasta" },
            { "number": 2, "description": "Mix eggs and cheese" }
          ],
          "servings": 4,
          "prepTimeMinutes": 10,
          "cookTimeMinutes": 15,
          "difficulty": "medium",
          "imageUrl": null
        }
        """;

    private readonly ILogger<SemanticKernelRecipeExtractionService> logger = Substitute.For<ILogger<SemanticKernelRecipeExtractionService>>();

    [Fact]
    public async Task ExtractAsync_ValidRecipeJson_ReturnsExtractedRecipe()
    {
        var sut = CreateSut(validRecipeJson);
        var content = new ScrapedContent("Some recipe text", new RecipeUrl("https://example.com/recipe"));

        var result = await sut.ExtractAsync(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Pasta Carbonara");
        result.Value.Description.Should().Be("A classic Italian dish");
        result.Value.Ingredients.Should().HaveCount(2);
        result.Value.Ingredients[0].Name.Should().Be("Spaghetti");
        result.Value.Ingredients[0].Amount.Should().Be(400);
        result.Value.Ingredients[0].Unit.Should().Be("g");
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Servings.Should().Be(4);
        result.Value.Difficulty.Should().Be("medium");
    }

    [Fact]
    public async Task ExtractAsync_JsonWrappedInMarkdownFence_ReturnsExtractedRecipe()
    {
        var wrapped = $"```json\n{validRecipeJson}\n```";
        var sut = CreateSut(wrapped);
        var content = new ScrapedContent("Some recipe text", new RecipeUrl("https://example.com/recipe"));

        var result = await sut.ExtractAsync(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Pasta Carbonara");
    }

    [Fact]
    public async Task ExtractAsync_JsonWrappedInPlainFence_ReturnsExtractedRecipe()
    {
        var wrapped = $"```\n{validRecipeJson}\n```";
        var sut = CreateSut(wrapped);
        var content = new ScrapedContent("Some recipe text", new RecipeUrl("https://example.com/recipe"));

        var result = await sut.ExtractAsync(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Pasta Carbonara");
    }

    [Fact]
    public async Task ExtractAsync_NoRecipeFoundResponse_ReturnsNoRecipeFound()
    {
        var sut = CreateSut("""{ "error": "NO_RECIPE_FOUND" }""");
        var content = new ScrapedContent("Random page text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
    }

    [Fact]
    public async Task ExtractAsync_InvalidJson_ReturnsExtractionFailed()
    {
        var sut = CreateSut("this is not valid JSON at all");
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    [Fact]
    public async Task ExtractAsync_LlmThrowsException_ReturnsExtractionFailed()
    {
        var sut = CreateSutWithException(new HttpRequestException("Service unavailable"));
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    [Fact]
    public async Task ExtractAsync_UserCancellation_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var sut = CreateSutWithException(new OperationCanceledException(cts.Token));
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var act = () => sut.ExtractAsync(content, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExtractAsync_MinimalRecipe_ReturnsExtractedRecipe()
    {
        var json = """
            {
              "title": "Simple Toast",
              "ingredients": [{ "name": "Bread", "amount": 2, "unit": "slices" }],
              "steps": [{ "number": 1, "description": "Toast the bread" }]
            }
            """;
        var sut = CreateSut(json);
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Simple Toast");
        result.Value.Description.Should().BeNull();
        result.Value.Servings.Should().BeNull();
        result.Value.Difficulty.Should().BeNull();
    }

    [Fact]
    public async Task ExtractAsync_EmptyContent_ReturnsExtractionFailed()
    {
        var sut = CreateSut(string.Empty);
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    [Fact]
    public async Task ExtractAsync_NullLlmContent_ReturnsExtractionFailed()
    {
        var sut = CreateSutWithNullContent();
        var content = new ScrapedContent("Some text", new RecipeUrl("https://example.com/page"));

        var result = await sut.ExtractAsync(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    private static Kernel CreateKernel(IChatCompletionService chatCompletionService)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatCompletionService);
        return builder.Build();
    }

    private SemanticKernelRecipeExtractionService CreateSut(string llmResponse)
    {
        var fake = new FakeChatCompletionService(llmResponse);
        var kernel = CreateKernel(fake);
        return new SemanticKernelRecipeExtractionService(kernel, logger);
    }

    private SemanticKernelRecipeExtractionService CreateSutWithException(Exception exception)
    {
        var fake = new FakeChatCompletionService(exception);
        var kernel = CreateKernel(fake);
        return new SemanticKernelRecipeExtractionService(kernel, logger);
    }

    private SemanticKernelRecipeExtractionService CreateSutWithNullContent()
    {
        var fake = new FakeChatCompletionService();
        var kernel = CreateKernel(fake);
        return new SemanticKernelRecipeExtractionService(kernel, logger);
    }

    private sealed class FakeChatCompletionService(
        string? response = null,
        Exception? exception = null) : IChatCompletionService
    {
        public FakeChatCompletionService(string response)
            : this(response, null)
        {
        }

        public FakeChatCompletionService(Exception exception)
            : this(null, exception)
        {
        }

        public IReadOnlyDictionary<string, object?> Attributes { get; } =
            new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            IReadOnlyList<ChatMessageContent> result =
                [new ChatMessageContent(AuthorRole.Assistant, response)];
            return Task.FromResult(result);
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
