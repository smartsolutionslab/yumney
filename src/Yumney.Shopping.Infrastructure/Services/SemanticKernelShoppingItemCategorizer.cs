using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;

#pragma warning disable SA1601
public sealed partial class SemanticKernelShoppingItemCategorizer(
	Kernel kernel,
	ILogger<SemanticKernelShoppingItemCategorizer> logger)
	: IShoppingItemCategorizer
{
#pragma warning disable SA1303
	private const string systemPrompt = """
		You are a grocery item categorizer. Given an item name, respond with exactly one of these categories:
		produce, dairy, meat-fish, bakery, frozen, beverages, pantry, spices, household, other

		Respond with only the category value, nothing else. No explanation, no punctuation.
		The item may be in English or German.

		Examples:
		"tahini" → pantry
		"Tofu" → produce
		"Alufolie" → household
		"Tiefkühlerbsen" → frozen
		"cinnamon" → spices
		"Zimt" → spices
		""";
#pragma warning restore SA1303

	public async Task<IngredientCategory> CategorizeAsync(ItemName name, CancellationToken cancellationToken = default)
	{
		var staticResult = IngredientCategoryResolver.Resolve(name.Value);
		if (staticResult is not null) return staticResult;

		try
		{
			var chat = new ChatHistory();
			chat.AddSystemMessage(systemPrompt);
			chat.AddUserMessage(name.Value);

			var completion = kernel.GetRequiredService<IChatCompletionService>();
			var result = await completion.GetChatMessageContentAsync(chat, cancellationToken: cancellationToken);
			var response = result.Content?.Trim().ToLowerInvariant() ?? string.Empty;

			return Parse(response, name.Value);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogCategorizationFailed(name.Value, ex.Message);
			return IngredientCategory.Other;
		}
	}

	public async Task<IReadOnlyDictionary<ItemName, IngredientCategory>> CategorizeManyAsync(
		IReadOnlyCollection<ItemName> names,
		CancellationToken cancellationToken = default)
	{
		var distinct = names.DistinctBy(n => n.Value).ToList();
		var tasks = distinct.Select(async name => (name, category: await CategorizeAsync(name, cancellationToken)));
		var results = await Task.WhenAll(tasks);
		return results.ToDictionary(pair => pair.name, pair => pair.category);
	}

	private IngredientCategory Parse(string response, string itemName)
	{
		try
		{
			return IngredientCategory.From(response);
		}
		catch
		{
			LogUnrecognizedCategory(itemName, response);
			return IngredientCategory.Other;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM categorization failed for '{ItemName}': {Reason}")]
	private partial void LogCategorizationFailed(string itemName, string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM returned unrecognized category for '{ItemName}': '{Response}'")]
	private partial void LogUnrecognizedCategory(string itemName, string response);
}
