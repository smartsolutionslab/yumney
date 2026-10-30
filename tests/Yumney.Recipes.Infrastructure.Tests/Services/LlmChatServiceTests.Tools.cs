using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;
using DomainChat = SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1601
public sealed partial class LlmChatServiceTests
#pragma warning restore SA1601
{
	[Fact]
	public async Task ChatAsync_OffersEveryChatToolByWireName()
	{
		var fake = new ToolCallingChatClient(ToolCallingChatClient.Turn.Reply("Hi!"));
		var (chatService, _) = BuildChatService(fake);

		await chatService.ChatAsync(DomainChat.ChatMessageContent.From("hi"), [], Owner());

		fake.CapturedOptions[0]!.Tools!.Select(tool => tool.Name).Should().BeEquivalentTo(
		[
			"search_recipes", "get_recipe", "get_cookable_recipes", "rate_recipe", "get_weekly_plan",
			"assign_meal", "confirm_meal_cooked", "get_merged_shopping_list", "create_shopping_list_from_recipes",
			"add_to_shopping_list", "remove_from_shopping_list", "swap_meal_slots", "clear_meal_slot",
		]);
	}

	[Fact]
	public void ToFunction_EveryTool_CarriesItsDescription()
	{
		var functions = BuildTools(new ChatToolContext()).Select(tool => tool.ToFunction());

		functions.Should().AllSatisfy(function => function.Description.Should().NotBeNullOrWhiteSpace());
	}

	[Fact]
	public void ToFunction_CancellationToken_IsNotPartOfTheSchema()
	{
		var search = BuildTools(new ChatToolContext()).OfType<SearchRecipesTool>().Single().ToFunction();

		search.JsonSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name)
			.Should().Equal("query");
	}

	[Fact]
	public async Task ChatAsync_ModelFails_ReturnsExtractionFailed()
	{
		var context = new ChatToolContext();
		var service = new LlmChatService(
			new ScriptedChatClient(new HttpRequestException("upstream down")),
			BuildTools(context),
			context,
			NullLogger<LlmChatService>.Instance);

		var result = await service.ChatAsync(DomainChat.ChatMessageContent.From("hi"), [], Owner());

		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task ChatAsync_ToolResultIsSentBackToTheModel()
	{
		var fake = new ToolCallingChatClient(
			ToolCallingChatClient.Turn.Call("search_recipes", new() { ["query"] = "pasta" }),
			ToolCallingChatClient.Turn.Reply("Done."));
		var (chatService, _) = BuildChatService(fake);

		var result = await chatService.ChatAsync(DomainChat.ChatMessageContent.From("find pasta"), [], Owner());

		result.Value.Reply.Should().Be("Done.");
		fake.CapturedOptions.Should().HaveCount(2, "the model is asked again once the tool result is available");
	}
}
