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
    RecipeUrl? SourceUrl = null) : ICommand<Result<SavedRecipeDto>>
{
    public static SaveRecipeCommand FromRequest(SaveRecipeRequest request)
    {
        return new SaveRecipeCommand(
            new RecipeTitle(request.Title),
            request.Ingredients.Select(i => new SaveRecipeIngredientCommand(
                new IngredientName(i.Name),
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))).ToList(),
            request.Steps.Select(s => new SaveRecipeStepCommand(
                new StepNumber(s.Number),
                new StepDescription(s.Description))).ToList(),
            RecipeDescription.FromNullable(request.Description),
            Servings.FromNullable(request.Servings),
            PreparationTime.FromNullable(request.PrepTimeMinutes),
            CookingTime.FromNullable(request.CookTimeMinutes),
            Difficulty.FromNullable(request.Difficulty),
            ImageUrl.FromNullable(request.ImageUrl),
            RecipeUrl.FromNullable(request.SourceUrl));
    }
}

public sealed record SaveRecipeIngredientCommand(IngredientName Name, Amount? Amount, Unit? Unit);

public sealed record SaveRecipeStepCommand(StepNumber Number, StepDescription Description);
