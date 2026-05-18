using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1601
public sealed partial class SemanticKernelChatServiceFunctionCallingTests
#pragma warning restore SA1601
{
	private static Result<PagedResult<RecipeListItemDto>> SuccessRecipes(params object[] idTitlePairs)
	{
		List<RecipeListItemDto> items = [];
		for (var index = 0; index < idTitlePairs.Length; index += 2)
		{
			var id = (Guid)idTitlePairs[index];
			var title = (string)idTitlePairs[index + 1];
			items.Add(new RecipeListItemDto(
				id,
				title,
				Description: null,
				Servings: 4,
				PrepTimeMinutes: 10,
				CookTimeMinutes: 20,
				Difficulty: null,
				ImageUrl: null,
				CreatedAt: DateTime.UtcNow,
				Tags: [],
				IsFavorite: false,
				Rating: null,
				HasNotes: false));
		}

		return Result<PagedResult<RecipeListItemDto>>.Success(new PagedResult<RecipeListItemDto>(
			items,
			TotalCount: items.Count,
			Page: 1,
			PageSize: 10));
	}

	private static Result<PagedResult<CookableRecipeDto>> SuccessCookable(Guid id, string title, CookableRecipeMatchTier tier)
	{
		var item = new CookableRecipeDto(
			id,
			title,
			ImageUrl: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			IngredientCount: 6,
			Tier: tier,
			MissingIngredients: []);
		return Result<PagedResult<CookableRecipeDto>>.Success(new PagedResult<CookableRecipeDto>(
			[item],
			TotalCount: 1,
			Page: 1,
			PageSize: 5));
	}

	private static Result<RecipeDetailDto> SuccessRecipeDetail(Guid id, string title) =>
		Result<RecipeDetailDto>.Success(new RecipeDetailDto(
			id,
			title,
			Description: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			ImageUrl: null,
			Language: null,
			SourceUrl: null,
			CreatedAt: DateTime.UtcNow,
			Ingredients: [],
			Steps: [],
			Tags: [],
			IsFavorite: false,
			Rating: null,
			Notes: null));

	/// <summary>
	/// Fake IChatCompletionService that emulates an LLM-with-function-calling
	/// in a single round trip: invoke the requested kernel functions itself
	/// (populating ChatToolContext via the real tool methods) and return the
	/// canned final reply. SK's auto-invoke loop is implemented inside each
	/// connector (OpenAI/Anthropic/Azure), not in the abstract; bypassing it
	/// here is intentional — what we test is OUR plumbing (tool registration,
	/// ChatToolContext aggregation, action emission), not the SDK's loop.
	/// </summary>
	private sealed class FakeChatCompletionService(
		IReadOnlyList<FunctionCallContent> functionCallsToInvoke,
		string finalReply) : IChatCompletionService
	{
		public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

		public async Task<IReadOnlyList<Microsoft.SemanticKernel.ChatMessageContent>> GetChatMessageContentsAsync(
			ChatHistory chatHistory,
			PromptExecutionSettings? executionSettings = null,
			Kernel? kernel = null,
			CancellationToken cancellationToken = default)
		{
			if (kernel is not null)
			{
				foreach (var call in functionCallsToInvoke)
				{
					var function = kernel.Plugins[call.PluginName!][call.FunctionName];
					await function.InvokeAsync(kernel, call.Arguments, cancellationToken);
				}
			}

			return [new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.Assistant, finalReply)];
		}

		public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
			ChatHistory chatHistory,
			PromptExecutionSettings? executionSettings = null,
			Kernel? kernel = null,
			CancellationToken cancellationToken = default) =>
			throw new NotImplementedException("Streaming not used by SemanticKernelChatService.");
	}
}
