using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public static class SaveRecipeErrors
{
    public static readonly ApiError AlreadyImported = new("RECIPE_ALREADY_IMPORTED", "This recipe has already been imported.", 409);
}
