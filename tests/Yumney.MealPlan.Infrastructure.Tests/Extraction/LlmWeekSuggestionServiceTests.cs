using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Extraction.Services;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Extraction;

public class LlmWeekSuggestionServiceTests
{
	private static readonly Guid Carbonara = Guid.NewGuid();
	private static readonly Guid Risotto = Guid.NewGuid();

	private static readonly IReadOnlyList<RecipeCatalogEntry> Catalog =
	[
		new(Carbonara, "Carbonara", 10, 15, null, [], IsFavorite: false, Rating: null),
		new(Risotto, "Risotto", 10, 30, null, [], IsFavorite: true, Rating: 5),
	];

	[Fact]
	public async Task SuggestAsync_ValidReply_ReturnsEntriesOrderedByDay()
	{
		var chatClient = new ScriptedChatClient(Reply(("Tuesday", Risotto), ("Monday", Carbonara)));

		var result = await SuggestAsync(chatClient);

		result.Value.Select(entry => (entry.Day, entry.RecipeTitle))
			.Should().Equal(("Monday", "Carbonara"), ("Tuesday", "Risotto"));
	}

	[Fact]
	public async Task SuggestAsync_RecipeOutsideCatalog_IsDropped()
	{
		var chatClient = new ScriptedChatClient(Reply(("Monday", Carbonara), ("Tuesday", Guid.NewGuid())));

		var result = await SuggestAsync(chatClient);

		result.Value.Should().ContainSingle().Which.RecipeIdentifier.Should().Be(Carbonara);
	}

	[Fact]
	public async Task SuggestAsync_ReplyInMarkdownFence_ReturnsEntries()
	{
		var chatClient = new ScriptedChatClient($"```json\n{Reply(("Monday", Carbonara))}\n```");

		var result = await SuggestAsync(chatClient);

		result.Value.Should().ContainSingle();
	}

	[Fact]
	public async Task SuggestAsync_MalformedThenValid_RetriesOnce()
	{
		var chatClient = new ScriptedChatClient("not json", Reply(("Monday", Carbonara)));

		var result = await SuggestAsync(chatClient);

		result.IsSuccess.Should().BeTrue();
		chatClient.InvocationCount.Should().Be(2);
	}

	[Fact]
	public async Task SuggestAsync_LlmFails_ReturnsSuggestionFailed()
	{
		var chatClient = new ScriptedChatClient(new HttpRequestException("upstream down"));

		var result = await SuggestAsync(chatClient);

		result.Error.Should().Be(SuggestWeekPlanErrors.SuggestionFailed);
	}

	[Fact]
	public async Task SuggestAsync_RequestsJsonSchemaOutput()
	{
		var chatClient = new ScriptedChatClient(Reply(("Monday", Carbonara)));

		await SuggestAsync(chatClient);

		chatClient.CapturedOptions!.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>()
			.Which.Schema.Should().NotBeNull();
	}

	private static Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(ScriptedChatClient chatClient)
	{
		var service = new LlmWeekSuggestionService(chatClient, NullLogger<LlmWeekSuggestionService>.Instance);
		return service.SuggestAsync(WeekIdentifier.From(2026, 44), Catalog, [], DietaryProfileSnapshot.Empty);
	}

	private static string Reply(params (string Day, Guid Recipe)[] entries) =>
		$$"""{"entries":[{{string.Join(",", entries.Select(entry => $$"""{"day":"{{entry.Day}}","recipeIdentifier":"{{entry.Recipe}}"}"""))}}]}""";
}
