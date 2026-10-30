using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1601
public sealed partial class LlmChatServiceTests
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
}
