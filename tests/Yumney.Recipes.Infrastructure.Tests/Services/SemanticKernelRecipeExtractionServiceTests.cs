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
#pragma warning disable SA1601
public partial class SemanticKernelRecipeExtractionServiceTests
#pragma warning restore SA1601
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
		var service = CreateService(validRecipeJson);
		var content = new ScrapedContent("Some recipe text", RecipeUrl.From("https://example.com/recipe"));

		var result = await service.ExtractAsync(content);

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
		var service = CreateService(wrapped);
		var content = new ScrapedContent("Some recipe text", RecipeUrl.From("https://example.com/recipe"));

		var result = await service.ExtractAsync(content);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public async Task ExtractAsync_JsonWrappedInPlainFence_ReturnsExtractedRecipe()
	{
		var wrapped = $"```\n{validRecipeJson}\n```";
		var service = CreateService(wrapped);
		var content = new ScrapedContent("Some recipe text", RecipeUrl.From("https://example.com/recipe"));

		var result = await service.ExtractAsync(content);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public async Task ExtractAsync_NoRecipeFoundResponse_ReturnsNoRecipeFound()
	{
		var service = CreateService("""{ "error": "NO_RECIPE_FOUND" }""");
		var content = new ScrapedContent("Random page text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
	}

	[Fact]
	public async Task ExtractAsync_InvalidJson_ReturnsExtractionFailed()
	{
		var service = CreateService("this is not valid JSON at all");
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task ExtractAsync_LlmThrowsException_ReturnsExtractionFailed()
	{
		var service = CreateServiceWithException(new HttpRequestException("Service unavailable"));
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task ExtractAsync_UserCancellation_ThrowsOperationCanceledException()
	{
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var service = CreateServiceWithException(new OperationCanceledException(cts.Token));
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var act = () => service.ExtractAsync(content, cts.Token);

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
		var service = CreateService(json);
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Simple Toast");
		result.Value.Description.Should().BeNull();
		result.Value.Servings.Should().BeNull();
		result.Value.Difficulty.Should().BeNull();
	}

	[Fact]
	public async Task ExtractAsync_EmptyContent_ReturnsExtractionFailed()
	{
		var service = CreateService(string.Empty);
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task ExtractAsync_NullLlmContent_ReturnsExtractionFailed()
	{
		var service = CreateServiceWithNullContent();
		var content = new ScrapedContent("Some text", RecipeUrl.From("https://example.com/page"));

		var result = await service.ExtractAsync(content);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	private static Kernel CreateKernel(IChatCompletionService chatCompletionService)
	{
		var builder = Kernel.CreateBuilder();
		builder.Services.AddSingleton(chatCompletionService);
		return builder.Build();
	}

	private SemanticKernelRecipeExtractionService CreateService(string llmResponse)
	{
		var fake = new FakeChatCompletionService(llmResponse);
		return CreateService(fake);
	}

	private SemanticKernelRecipeExtractionService CreateService(FakeChatCompletionService fake)
	{
		var kernel = CreateKernel(fake);
		return new SemanticKernelRecipeExtractionService(kernel, new InMemoryExtractionResultCache(), logger);
	}

	private SemanticKernelRecipeExtractionService CreateServiceWithException(Exception exception)
	{
		var fake = new FakeChatCompletionService(exception);
		var kernel = CreateKernel(fake);
		return new SemanticKernelRecipeExtractionService(kernel, new InMemoryExtractionResultCache(), logger);
	}

	private SemanticKernelRecipeExtractionService CreateServiceWithNullContent()
	{
		var fake = new FakeChatCompletionService();
		var kernel = CreateKernel(fake);
		return new SemanticKernelRecipeExtractionService(kernel, new InMemoryExtractionResultCache(), logger);
	}
}
