using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

internal static class ChatHitMappingExtensions
{
	public static RecipeChatHit ToChatHit(this RecipeListItemDto item) =>
		new(item.Identifier, item.Title, item.Description);

	public static CookableRecipeChatHit ToChatHit(this CookableRecipeDto item) =>
		new(item.RecipeIdentifier, item.Title, item.Tier.ToString(), item.MissingIngredients);
}
