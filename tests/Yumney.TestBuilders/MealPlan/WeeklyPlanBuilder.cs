using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using WeeklyPlanAggregate = SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.TestBuilders.MealPlan;

public sealed class WeeklyPlanBuilder
{
	private OwnerIdentifier owner = OwnerIdentifier.From("user-123");
	private WeekIdentifier week = WeekIdentifier.From(2026, 17);
	private SlotServings? defaultServings;
	private bool extendedMode;

	public static WeeklyPlanBuilder A() => new();

	public WeeklyPlanBuilder OwnedBy(string ownerId) => OwnedBy(OwnerIdentifier.From(ownerId));

	public WeeklyPlanBuilder OwnedBy(OwnerIdentifier value)
	{
		owner = value;
		return this;
	}

	public WeeklyPlanBuilder ForWeek(int year, int weekNumber)
	{
		week = WeekIdentifier.From(year, weekNumber);
		return this;
	}

	public WeeklyPlanBuilder ForWeek(WeekIdentifier value)
	{
		week = value;
		return this;
	}

	public WeeklyPlanBuilder WithDefaultServings(int value)
	{
		defaultServings = SlotServings.From(value);
		return this;
	}

	public WeeklyPlanBuilder WithDefaultServings(SlotServings value)
	{
		defaultServings = value;
		return this;
	}

	public WeeklyPlanBuilder InExtendedMode()
	{
		extendedMode = true;
		return this;
	}

	public WeeklyPlanAggregate Build()
	{
		var plan = WeeklyPlanAggregate.Create(owner, week, defaultServings);
		if (extendedMode)
		{
			plan.EnableExtendedMode(defaultServings);
		}

		return plan;
	}
}
