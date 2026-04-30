using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1311
public class SemanticKernelRecipeSuggestionServiceTests
{
	private static readonly string validRecipesJson = """
        {
          "recipes": [
            {
              "title": "Apple Pie",
              "ingredients": [{ "name": "Apple", "amount": 5, "unit": null }],
              "steps": [{ "number": 1, "description": "Bake" }],
              "language": "en"
            },
            {
              "title": "Apple Crumble",
              "ingredients": [{ "name": "Apple", "amount": 4, "unit": null }],
              "steps": [{ "number": 1, "description": "Crumble" }],
              "language": "en"
            }
          ]
        }
        """;

	private readonly ILogger<SemanticKernelRecipeSuggestionService> logger = Substitute.For<ILogger<SemanticKernelRecipeSuggestionService>>();

	[Fact]
	public async Task SuggestAsync_ValidJson_ReturnsRecipes()
	{
		var sut = CreateSut(validRecipesJson);

		var result = await sut.SuggestAsync(["apple"], "vegetarian", [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value[0].Title.Should().Be("Apple Pie");
	}

	[Fact]
	public async Task SuggestAsync_JsonWrappedInMarkdownFence_ReturnsRecipes()
	{
		var sut = CreateSut($"```json\n{validRecipesJson}\n```");

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task SuggestAsync_EmptyRecipesArray_ReturnsSuggestionFailed()
	{
		var sut = CreateSut("""{ "recipes": [] }""");

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_AllRecipesMissingFields_ReturnsSuggestionFailed()
	{
		var sut = CreateSut("""
            {
              "recipes": [
                { "title": "Foo" },
                { "title": "Bar", "ingredients": [], "steps": [] }
              ]
            }
            """);

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_MixOfUsableAndMalformed_ReturnsOnlyUsable()
	{
		var sut = CreateSut("""
            {
              "recipes": [
                { "title": "Foo" },
                {
                  "title": "Apple Pie",
                  "ingredients": [{ "name": "Apple", "amount": 5, "unit": null }],
                  "steps": [{ "number": 1, "description": "Bake" }],
                  "language": "en"
                }
              ]
            }
            """);

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainSingle().Which.Title.Should().Be("Apple Pie");
	}

	[Fact]
	public async Task SuggestAsync_MalformedThenValid_RetriesAndSucceeds()
	{
		var fake = new FakeChatCompletionService(["I can't do JSON today.", validRecipesJson]);
		var sut = CreateSut(fake);

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		fake.InvocationCount.Should().Be(2);
	}

	[Fact]
	public async Task SuggestAsync_MalformedTwice_ReturnsSuggestionFailed()
	{
		var fake = new FakeChatCompletionService(["garbage 1", "garbage 2"]);
		var sut = CreateSut(fake);

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
		fake.InvocationCount.Should().Be(2);
	}

	[Fact]
	public async Task SuggestAsync_LlmThrows_ReturnsSuggestionFailed()
	{
		var fake = new FakeChatCompletionService(new HttpRequestException("upstream down"));
		var sut = CreateSut(fake);

		var result = await sut.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_ForwardsAvailableIngredientsAndDietary()
	{
		var fake = new FakeChatCompletionService(validRecipesJson);
		var sut = CreateSut(fake);

		await sut.SuggestAsync(["chicken", "rice"], "vegetarian", ["gluten-free"], 3);

		var userMessage = fake.CapturedHistory!.Last(m => m.Role == AuthorRole.User).Content!;
		userMessage.Should().Contain("chicken");
		userMessage.Should().Contain("rice");
		userMessage.Should().Contain("vegetarian");
		userMessage.Should().Contain("gluten-free");
		userMessage.Should().Contain("<count>3</count>");
	}

	[Fact]
	public async Task SuggestAsync_NullDietary_RendersAsNoPreference()
	{
		var fake = new FakeChatCompletionService(validRecipesJson);
		var sut = CreateSut(fake);

		await sut.SuggestAsync(["apple"], null, [], 1);

		var userMessage = fake.CapturedHistory!.Last(m => m.Role == AuthorRole.User).Content!;
		userMessage.Should().Contain("(no preference)");
		userMessage.Should().Contain("(none)");
	}

	[Fact]
	public async Task SuggestAsync_CancelledToken_PropagatesOperationCanceled()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		var fake = new FakeChatCompletionService(validRecipesJson);
		var sut = CreateSut(fake);

		var act = () => sut.SuggestAsync(["apple"], null, [], 2, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private SemanticKernelRecipeSuggestionService CreateSut(string llmResponse) =>
		CreateSut(new FakeChatCompletionService(llmResponse));

	private SemanticKernelRecipeSuggestionService CreateSut(FakeChatCompletionService fake)
	{
		var builder = Kernel.CreateBuilder();
		builder.Services.AddSingleton<IChatCompletionService>(fake);
		return new SemanticKernelRecipeSuggestionService(builder.Build(), logger);
	}

	private sealed class FakeChatCompletionService : IChatCompletionService
	{
		private readonly string? response;
		private readonly Exception? exception;
		private readonly Queue<string>? responseQueue;

		public FakeChatCompletionService(string response)
		{
			this.response = response;
		}

		public FakeChatCompletionService(Exception exception)
		{
			this.exception = exception;
		}

		public FakeChatCompletionService(IEnumerable<string> responses)
		{
			responseQueue = new Queue<string>(responses);
		}

		public ChatHistory? CapturedHistory { get; private set; }

		public int InvocationCount { get; private set; }

		public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

		public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
			ChatHistory chatHistory,
			PromptExecutionSettings? executionSettings = null,
			Kernel? kernel = null,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			CapturedHistory = new ChatHistory(chatHistory);
			InvocationCount++;

			if (exception is not null) throw exception;

			var content = responseQueue is { Count: > 0 } ? responseQueue.Dequeue() : response;
			IReadOnlyList<ChatMessageContent> result = [new(AuthorRole.Assistant, content)];
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
