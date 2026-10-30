using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Extraction.Services;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Extraction;

public class LlmShoppingItemCategorizerTests
{
	private static readonly ItemName Milk = ItemName.From("milk");
	private static readonly ItemName Tahini = ItemName.From("tahini");
	private static readonly ItemName Sumac = ItemName.From("sumac");

	[Fact]
	public async Task CategorizeManyAsync_SeveralUnknownItems_AsksLlmOnce()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "pantry"), ("sumac", "spices")));

		await Categorizer(chatClient).CategorizeManyAsync([Tahini, Sumac]);

		chatClient.InvocationCount.Should().Be(1);
	}

	[Fact]
	public async Task CategorizeManyAsync_UnknownItems_UsesLlmCategories()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "pantry"), ("sumac", "spices")));

		var categories = await Categorizer(chatClient).CategorizeManyAsync([Tahini, Sumac]);

		categories.Should().Equal(new Dictionary<ItemName, IngredientCategory>
		{
			[Tahini] = IngredientCategory.Pantry,
			[Sumac] = IngredientCategory.Spices,
		});
	}

	[Fact]
	public async Task CategorizeManyAsync_StaticallyKnownItem_IsNotSentToLlm()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "pantry")));

		await Categorizer(chatClient).CategorizeManyAsync([Milk, Tahini]);

		chatClient.LastUserText.Should().NotContain("milk");
	}

	[Fact]
	public async Task CategorizeManyAsync_OnlyStaticallyKnownItems_SkipsLlm()
	{
		var chatClient = new ScriptedChatClient(Reply());

		var categories = await Categorizer(chatClient).CategorizeManyAsync([Milk]);

		chatClient.InvocationCount.Should().Be(0);
		categories[Milk].Should().Be(IngredientCategory.Dairy);
	}

	[Fact]
	public async Task CategorizeManyAsync_ItemMissingFromReply_FallsBackToOther()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "pantry")));

		var categories = await Categorizer(chatClient).CategorizeManyAsync([Tahini, Sumac]);

		categories[Sumac].Should().Be(IngredientCategory.Other);
	}

	[Fact]
	public async Task CategorizeManyAsync_UnknownCategoryInReply_FallsBackToOther()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "condiments")));

		var categories = await Categorizer(chatClient).CategorizeManyAsync([Tahini]);

		categories[Tahini].Should().Be(IngredientCategory.Other);
	}

	[Fact]
	public async Task CategorizeManyAsync_LlmFails_FallsBackToOther()
	{
		var chatClient = new ScriptedChatClient(new HttpRequestException("upstream down"));

		var categories = await Categorizer(chatClient).CategorizeManyAsync([Tahini]);

		categories[Tahini].Should().Be(IngredientCategory.Other);
	}

	[Fact]
	public async Task CategorizeManyAsync_RequestsJsonSchemaOutput()
	{
		var chatClient = new ScriptedChatClient(Reply(("tahini", "pantry")));

		await Categorizer(chatClient).CategorizeManyAsync([Tahini]);

		chatClient.CapturedOptions!.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>()
			.Which.Schema.Should().NotBeNull();
	}

	[Fact]
	public async Task CategorizeAsync_UnknownItem_UsesLlmCategory()
	{
		var chatClient = new ScriptedChatClient(Reply(("Tahini", "pantry")));

		var category = await Categorizer(chatClient).CategorizeAsync(Tahini);

		category.Should().Be(IngredientCategory.Pantry);
	}

	private static LlmShoppingItemCategorizer Categorizer(ScriptedChatClient chatClient) =>
		new(chatClient, NullLogger<LlmShoppingItemCategorizer>.Instance);

	private static string Reply(params (string Name, string Category)[] items) =>
		$$"""{"items":[{{string.Join(",", items.Select(item => $$"""{"name":"{{item.Name}}","category":"{{item.Category}}"}"""))}}]}""";
}
