using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RequestMappingExtensions
{
    public static SaveRecipeIngredientItem ToCommandItem(this SaveRecipeIngredientRequest request)
    {
        return new SaveRecipeIngredientItem(
            new IngredientName(request.Name),
            Amount.FromNullable(request.Amount),
            Unit.FromNullable(request.Unit));
    }

    public static SaveRecipeStepItem ToCommandItem(this SaveRecipeStepRequest request)
    {
        return new SaveRecipeStepItem(
            new StepNumber(request.Number),
            new StepDescription(request.Description));
    }
}
