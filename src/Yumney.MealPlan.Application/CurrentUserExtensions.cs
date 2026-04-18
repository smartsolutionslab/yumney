using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Application;

public static class CurrentUserExtensions
{
	public static OwnerIdentifier AsOwner(this ICurrentUser currentUser) =>
		OwnerIdentifier.From(currentUser.UserId);
}
