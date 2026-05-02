using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IRecipeIngredientLookup
{
	Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(
		SlotRecipeIdentifier recipe,
		CancellationToken cancellationToken = default);
}
