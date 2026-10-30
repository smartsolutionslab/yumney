using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmShoppingItemCategorizer(IChatClient chatClient, ILogger<LlmShoppingItemCategorizer> logger)
	: IShoppingItemCategorizer
{
#pragma warning disable SA1303
	private const string systemPrompt = """
		You are a grocery item categorizer. Assign each item in <items> (one per line) exactly one of these categories:
		produce, dairy, meat-fish, bakery, frozen, beverages, pantry, spices, household, other

		The items may be in English or German. Respond with JSON: one entry per input item,
		with "name" copied verbatim from the input and "category" set to one of the values above.

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
		var categories = await CategorizeManyAsync([name], cancellationToken);
		return categories[name];
	}

	public async Task<IReadOnlyDictionary<ItemName, IngredientCategory>> CategorizeManyAsync(
		IReadOnlyCollection<ItemName> names,
		CancellationToken cancellationToken = default)
	{
		var distinct = names.DistinctBy(name => name.Value).ToList();
		Dictionary<ItemName, IngredientCategory> categories = [];
		List<ItemName> unresolved = [];
		foreach (var name in distinct)
		{
			var staticCategory = IngredientCategoryResolver.Resolve(name.Value);
			if (staticCategory is null)
			{
				unresolved.Add(name);
			}
			else
			{
				categories[name] = staticCategory;
			}
		}

		if (unresolved.Count == 0) return categories;

		var llmCategories = await AskLlmAsync(unresolved, cancellationToken);
		foreach (var name in unresolved)
		{
			categories[name] = llmCategories.GetValueOrDefault(name.Value) ?? IngredientCategory.Other;
		}

		return categories;
	}

	// One request for the whole list instead of one per item; anything the
	// model skips or mislabels falls back to "other" in the caller.
	private async Task<Dictionary<string, IngredientCategory>> AskLlmAsync(List<ItemName> names, CancellationToken cancellationToken)
	{
		List<ChatMessage> messages =
		[
			new(ChatRole.System, systemPrompt),
			new(ChatRole.User, $"<items>\n{string.Join('\n', names.Select(name => name.Value))}\n</items>"),
		];

		try
		{
			var response = await chatClient.GetResponseAsync(messages, LlmJson.OptionsFor<CategorizationResponse>(), cancellationToken);
			var parsed = LlmJson.Deserialize<CategorizationResponse>(response.Text);
			return ToCategories(parsed?.Items ?? []);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogCategorizationFailed(names.Count, ex.Message);
			return new Dictionary<string, IngredientCategory>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private Dictionary<string, IngredientCategory> ToCategories(IReadOnlyList<CategorizedItem> items)
	{
		Dictionary<string, IngredientCategory> categories = new(StringComparer.OrdinalIgnoreCase);
		foreach (var (name, category) in items)
		{
			if (string.IsNullOrWhiteSpace(name)) continue;

			var normalized = category?.Trim().ToLowerInvariant();
			var parsed = IngredientCategory.All.FirstOrDefault(candidate => candidate.Value == normalized);
			if (parsed is null)
			{
				LogUnrecognizedCategory(name, category ?? string.Empty);
				continue;
			}

			categories.TryAdd(name.Trim(), parsed);
		}

		return categories;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM categorization failed for {ItemCount} item(s): {Reason}")]
	private partial void LogCategorizationFailed(int itemCount, string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM returned unrecognized category for '{ItemName}': '{Response}'")]
	private partial void LogUnrecognizedCategory(string itemName, string response);

#pragma warning disable SA1201, SA1202
	internal sealed record CategorizationResponse(IReadOnlyList<CategorizedItem>? Items);

	internal sealed record CategorizedItem(string? Name, string? Category);
#pragma warning restore SA1201, SA1202
}
