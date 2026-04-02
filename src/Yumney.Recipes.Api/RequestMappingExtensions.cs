using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RequestMappingExtensions
{
    public static SaveRecipeIngredientItem ToCommandItem(this SaveRecipeIngredientRequest request)
    {
        var (name, amount, unit) = request;

        return new SaveRecipeIngredientItem(
            IngredientName.From(name),
            Quantity.FromNullable(Amount.FromNullable(amount), Unit.FromNullable(unit)));
    }

    public static SaveRecipeStepItem ToCommandItem(this SaveRecipeStepRequest request)
    {
        var (number, description) = request;

        return new SaveRecipeStepItem(StepNumber.From(number), StepDescription.From(description));
    }
}
