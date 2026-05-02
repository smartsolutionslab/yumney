using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipe(
	string Title,
	string? Description,
	List<SaveRecipeIngredient> Ingredients,
	List<SaveRecipeStep> Steps,
	int? Servings,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	string? ImageUrl,
	string? Language = null,
	string? SourceUrl = null,
	List<string>? Tags = null)
{
	public void Deconstruct(
		out RecipeTitle title,
		out IReadOnlyList<SaveRecipeIngredientItem> ingredients,
		out IReadOnlyList<SaveRecipeStepItem> steps,
		out RecipeDescription? description,
		out Servings? servings,
		out TimingInfo? timing,
		out Difficulty? difficulty,
		out ImageUrl? imageUrl,
		out RecipeLanguage? language,
		out RecipeUrl? sourceUrl,
		out IReadOnlyList<RecipeTag>? tags)
	{
		title = RecipeTitle.From(Title);
		ingredients = Ingredients.MapToRecipeIngredientItems().ToList();
		steps = Steps.MapToRecipeStepItems().ToList();
		description = RecipeDescription.FromNullable(Description);
		servings = Domain.Recipe.Servings.FromNullable(Servings);
		timing = TimingInfo.FromNullable(
			PreparationTime.FromNullable(PrepTimeMinutes),
			CookingTime.FromNullable(CookTimeMinutes));
		difficulty = Domain.Recipe.Difficulty.FromNullable(Difficulty);
		imageUrl = Domain.Recipe.ImageUrl.FromNullable(ImageUrl);
		language = RecipeLanguage.FromNullable(Language);
		sourceUrl = RecipeUrl.FromNullable(SourceUrl);
		tags = Tags?.Select(tag => RecipeTag.From(tag)).ToList();
	}
}
