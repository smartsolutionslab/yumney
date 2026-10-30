using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class LlmIntentParserServiceTests
{
	[Fact]
	public async Task ParseAsync_ValidReply_ReturnsIntentAndEntities()
	{
		var chatClient = new ScriptedChatClient("""{"intent":"Add_To_List","entities":{"item":"milk"},"clarification":null}""");

		var result = await Parser(chatClient).ParseAsync("add milk", null);

		result.Value.Intent.Should().Be("add_to_list");
		result.Value.Entities.Should().Contain("ITEM", "milk");
	}

	[Fact]
	public async Task ParseAsync_InvalidJson_FallsBackToGeneralChat()
	{
		var result = await Parser(new ScriptedChatClient("I think you want milk")).ParseAsync("add milk", null);

		result.Value.Intent.Should().Be("general_chat");
	}

	[Fact]
	public async Task ParseAsync_LlmFails_Returns502()
	{
		var result = await Parser(new ScriptedChatClient(new HttpRequestException("down"))).ParseAsync("add milk", null);

		result.Error!.HttpStatusCode.Should().Be(502);
	}

	[Fact]
	public async Task ParseAsync_WithPageContext_PrefixesUserMessage()
	{
		var chatClient = new ScriptedChatClient("""{"intent":"add_to_list","entities":{}}""");

		await Parser(chatClient).ParseAsync("add milk", "shopping-list");

		chatClient.LastUserText.Should().Be("[Page context: shopping-list] add milk");
	}

	private static LlmIntentParserService Parser(ScriptedChatClient chatClient) =>
		new(chatClient, NullLogger<LlmIntentParserService>.Instance);
}
