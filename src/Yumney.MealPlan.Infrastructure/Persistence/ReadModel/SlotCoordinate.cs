using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Composite key that uniquely identifies a single read-model slot row. Carrying it
/// as one parameter keeps the projection-handler helpers under CLAUDE.md's 4-param
/// limit and stops callers from accidentally swapping the owner / week / day / meal
/// columns at a call site — every component is a typed value object (or enum), so
/// the compiler rejects an out-of-order argument list before it reaches the SQL.
/// </summary>
internal sealed record SlotCoordinate(OwnerIdentifier Owner, WeekIdentifier Week, DayOfWeek Day, MealType MealType)
{
	internal string OwnerValue => Owner.Value;

	internal string WeekValue => Week.Value;

	internal string DayName => Day.ToString();

	internal string MealTypeName => MealType.ToString();
}
