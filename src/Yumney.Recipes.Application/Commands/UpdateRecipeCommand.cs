using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record UpdateRecipeCommand(
    RecipeIdentifier Identifier,
    RecipeTitle Title,
    IReadOnlyList<SaveRecipeIngredientItem> Ingredients,
    IReadOnlyList<SaveRecipeStepItem> Steps,
    RecipeDescription? Description = null,
    Servings? Servings = null,
    PreparationTime? PreparationTime = null,
    CookingTime? CookingTime = null,
    Difficulty? Difficulty = null,
    ImageUrl? ImageUrl = null,
    IReadOnlyList<RecipeTag>? Tags = null) : ICommand<Result<RecipeDetailDto>>;
