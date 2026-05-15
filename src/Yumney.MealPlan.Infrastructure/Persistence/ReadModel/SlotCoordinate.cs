using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Composite key that uniquely identifies a single read-model slot row. Carrying it
/// as one parameter keeps the projection-handler helpers under CLAUDE.md's 4-param
/// limit and stops callers from accidentally swapping the owner / week / day / meal
/// columns at a call site (all four are typed differently than they were when
/// passed individually as four strings).
/// </summary>
internal sealed record SlotCoordinate(string OwnerId, string Week, DayOfWeek Day, MealType MealType)
{
	internal string DayName => Day.ToString();

	internal string MealTypeName => MealType.ToString();
}
