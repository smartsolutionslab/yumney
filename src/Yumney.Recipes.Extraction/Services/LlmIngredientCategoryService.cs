using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmIngredientCategoryService(
	[FromKeyedServices(LlmTasks.Fast)] IChatClient chatClient,
	ILogger<LlmIngredientCategoryService> logger)
	: IIngredientCategoryService
{
#pragma warning disable SA1303
	private const string systemPrompt = """
        You are a grocery item categorizer. Given an item name, respond with exactly one of these categories:
        produce, dairy, meat-fish, bakery, frozen, beverages, pantry, household, other

        Respond with only the category value, nothing else. No explanation, no punctuation.
        The item may be in English or German.

        Examples:
        "tahini" → pantry
        "Tofu" → produce
        "Alufolie" → household
        "Tiefkühlerbsen" → frozen
        """;
#pragma warning restore SA1303

	public async Task<IngredientCategory> CategorizeAsync(string itemName, CancellationToken cancellationToken = default)
	{
		var staticResult = IngredientCategoryResolver.Resolve(itemName);
		if (staticResult is not null) return staticResult;

		try
		{
			List<ChatMessage> messages = [new(ChatRole.System, systemPrompt), new(ChatRole.User, itemName)];
			var result = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
			var response = result.Text.Trim().ToLowerInvariant();

			return ParseCategory(response, itemName);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogCategorizationFailed(itemName, ex.Message);
			return IngredientCategory.Other;
		}
	}

	private IngredientCategory ParseCategory(string response, string itemName)
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
