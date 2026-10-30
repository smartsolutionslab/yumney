using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1311
public class LlmRecipeSuggestionServiceTests
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

	private readonly ILogger<LlmRecipeSuggestionService> logger = Substitute.For<ILogger<LlmRecipeSuggestionService>>();

	[Fact]
	public async Task SuggestAsync_ValidJson_ReturnsRecipes()
	{
		var service = CreateService(validRecipesJson);

		var result = await service.SuggestAsync(["apple"], "vegetarian", [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value[0].Title.Should().Be("Apple Pie");
	}

	[Fact]
	public async Task SuggestAsync_JsonWrappedInMarkdownFence_ReturnsRecipes()
	{
		var service = CreateService($"```json\n{validRecipesJson}\n```");

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task SuggestAsync_EmptyRecipesArray_ReturnsSuggestionFailed()
	{
		var service = CreateService("""{ "recipes": [] }""");

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_AllRecipesMissingFields_ReturnsSuggestionFailed()
	{
		var service = CreateService("""
            {
              "recipes": [
                { "title": "Foo" },
                { "title": "Bar", "ingredients": [], "steps": [] }
              ]
            }
            """);

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_MixOfUsableAndMalformed_ReturnsOnlyUsable()
	{
		var service = CreateService("""
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

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainSingle().Which.Title.Should().Be("Apple Pie");
	}

	[Fact]
	public async Task SuggestAsync_MalformedThenValid_RetriesAndSucceeds()
	{
		var fake = new ScriptedChatClient("I can't do JSON today.", validRecipesJson);
		var service = CreateService(fake);

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsSuccess.Should().BeTrue();
		fake.InvocationCount.Should().Be(2);
	}

	[Fact]
	public async Task SuggestAsync_MalformedTwice_ReturnsSuggestionFailed()
	{
		var fake = new ScriptedChatClient("garbage 1", "garbage 2");
		var service = CreateService(fake);

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
		fake.InvocationCount.Should().Be(2);
	}

	[Fact]
	public async Task SuggestAsync_LlmThrows_ReturnsSuggestionFailed()
	{
		var fake = new ScriptedChatClient(new HttpRequestException("upstream down"));
		var service = CreateService(fake);

		var result = await service.SuggestAsync(["apple"], null, [], 2);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_ForwardsAvailableIngredientsAndDietary()
	{
		var fake = new ScriptedChatClient(validRecipesJson);
		var service = CreateService(fake);

		await service.SuggestAsync(["chicken", "rice"], "vegetarian", ["gluten-free"], 3);

		var userMessage = fake.LastUserText;
		userMessage.Should().Contain("chicken");
		userMessage.Should().Contain("rice");
		userMessage.Should().Contain("vegetarian");
		userMessage.Should().Contain("gluten-free");
		userMessage.Should().Contain("<count>3</count>");
	}

	[Fact]
	public async Task SuggestAsync_NullDietary_RendersAsNoPreference()
	{
		var fake = new ScriptedChatClient(validRecipesJson);
		var service = CreateService(fake);

		await service.SuggestAsync(["apple"], null, [], 1);

		var userMessage = fake.LastUserText;
		userMessage.Should().Contain("(no preference)");
		userMessage.Should().Contain("(none)");
	}

	[Fact]
	public async Task SuggestAsync_CancelledToken_PropagatesOperationCanceled()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		var fake = new ScriptedChatClient(validRecipesJson);
		var service = CreateService(fake);

		var act = () => service.SuggestAsync(["apple"], null, [], 2, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private LlmRecipeSuggestionService CreateService(string llmResponse) =>
		CreateService(new ScriptedChatClient(llmResponse));

	private LlmRecipeSuggestionService CreateService(ScriptedChatClient fake) => new(fake, logger);
}
