namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record AssignRecipeRequest(
    DayOfWeek Day,
    Guid RecipeIdentifier,
    string RecipeTitle,
    int? Servings = null);
