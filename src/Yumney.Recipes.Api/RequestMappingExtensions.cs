using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RequestMappingExtensions
{
    public static SaveRecipeIngredientItem ToCommandItem(this SaveRecipeIngredientRequest request)
    {
        return new SaveRecipeIngredientItem(
            IngredientName.From(request.Name),
            Amount.FromNullable(request.Amount),
            Unit.FromNullable(request.Unit));
    }

    public static SaveRecipeStepItem ToCommandItem(this SaveRecipeStepRequest request)
    {
        return new SaveRecipeStepItem(
            StepNumber.From(request.Number),
            StepDescription.From(request.Description));
    }
}
