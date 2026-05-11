using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

// Day / RecipeIdentifier / RecipeTitle are JsonRequired so that a missing
// field on the wire surfaces as a 400 BadRequest from the JSON binder rather
// than a successful bind to the enum/Guid/string default (DayOfWeek.Sunday,
// Guid.Empty, null). Without this, the MCP assign_meal tool was happily
// planning to Sunday/Dinner when callers omitted `day`, and that side-effect
// also polluted the same week's state for follow-up contract tests.
public sealed record AssignRecipe(
	[property: JsonRequired] DayOfWeek Day,
	[property: JsonRequired] Guid RecipeIdentifier,
	[property: JsonRequired] string RecipeTitle,
	MealType MealType = MealType.Dinner,
	int? Servings = null);
