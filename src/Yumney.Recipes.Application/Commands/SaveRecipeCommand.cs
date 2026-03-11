using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeCommand(
    RecipeTitle Title,
    IReadOnlyList<SaveRecipeIngredientCommand> Ingredients,
    IReadOnlyList<SaveRecipeStepCommand> Steps,
    RecipeDescription? Description = null,
    Servings? Servings = null,
    PreparationTime? PreparationTime = null,
    CookingTime? CookingTime = null,
    Difficulty? Difficulty = null,
    ImageUrl? ImageUrl = null,
    RecipeUrl? SourceUrl = null) : ICommand<Result<SavedRecipeDto>>;

public sealed record SaveRecipeIngredientCommand(IngredientName Name, Amount? Amount, Unit? Unit);

public sealed record SaveRecipeStepCommand(StepNumber Number, StepDescription Description);
