using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeCommand(
	RecipeTitle Title,
	IReadOnlyList<SaveRecipeIngredientItem> Ingredients,
	IReadOnlyList<SaveRecipeStepItem> Steps,
	RecipeDescription? Description = null,
	Servings? Servings = null,
	TimingInfo? Timing = null,
	Difficulty? Difficulty = null,
	ImageUrl? ImageUrl = null,
	RecipeLanguage? Language = null,
	RecipeUrl? SourceUrl = null,
	IReadOnlyList<RecipeTag>? Tags = null) : ICommand<Result<SavedRecipeDto>>;
