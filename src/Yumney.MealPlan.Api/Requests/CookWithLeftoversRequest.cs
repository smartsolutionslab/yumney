using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record CookWithLeftoversRequest(
    DayOfWeek CookDay,
    Guid RecipeIdentifier,
    string RecipeTitle,
    int TotalServings,
    int EatServings,
    DayOfWeek LeftoverDay,
    MealType MealType = MealType.Dinner);
