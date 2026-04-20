using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

#pragma warning disable SA1311
public sealed class WeeklyPlan : AggregateRoot<WeeklyPlanIdentifier>
{
	private static readonly DayOfWeek[] allDays =
	[
		DayOfWeek.Monday,
		DayOfWeek.Tuesday,
		DayOfWeek.Wednesday,
		DayOfWeek.Thursday,
		DayOfWeek.Friday,
		DayOfWeek.Saturday,
		DayOfWeek.Sunday
	];

	private readonly List<MealSlot> slots = [];

	public OwnerIdentifier Owner { get; private set; } = default!;

	public WeekIdentifier Week { get; private set; } = default!;

	public bool IsExtendedMode { get; private set; }

	public IReadOnlyList<MealSlot> Slots => slots.AsReadOnly();

	private WeeklyPlan()
	{
	}

	public static WeeklyPlan Create(OwnerIdentifier owner, WeekIdentifier week, SlotServings? defaultServings = null)
	{
		var servings = defaultServings ?? SlotServings.Default();
		var plan = new WeeklyPlan
		{
			Id = WeeklyPlanIdentifier.New(),
			Owner = owner,
			Week = week,
		};

		foreach (var day in allDays)
		{
			plan.slots.Add(MealSlot.Create(day, MealType.Dinner, servings));
		}

		return plan;
	}

	public WeeklyPlan EnableExtendedMode(SlotServings? defaultServings = null)
	{
		if (IsExtendedMode) return this;

		var servings = defaultServings ?? SlotServings.Default();
		foreach (var day in allDays)
		{
			if (!slots.Any(s => s.Day == day && s.MealType == MealType.Breakfast))
			{
				slots.Add(MealSlot.Create(day, MealType.Breakfast, servings));
			}

			if (!slots.Any(s => s.Day == day && s.MealType == MealType.Lunch))
			{
				slots.Add(MealSlot.Create(day, MealType.Lunch, servings));
			}
		}

		IsExtendedMode = true;
		return this;
	}

	public WeeklyPlan DisableExtendedMode()
	{
		IsExtendedMode = false;
		return this;
	}

	public IReadOnlyList<MealSlot> GetVisibleSlots()
	{
		return IsExtendedMode
			? slots.AsReadOnly()
			: slots.Where(s => s.MealType == MealType.Dinner).ToList().AsReadOnly();
	}

	public WeeklyPlan AssignRecipe(
		DayOfWeek day,
		SlotRecipeReference recipe,
		MealType mealType = MealType.Dinner,
		SlotServings? servings = null)
	{
		var slot = FindSlot(day, mealType);
		slot.AssignRecipe(recipe, servings);
		return this;
	}

	public WeeklyPlan SetFreetext(DayOfWeek day, FreetextLabel label, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.SetAsFreetext(label);
		return this;
	}

	public WeeklyPlan SetLeftover(
		DayOfWeek day,
		DayOfWeek sourceDay,
		MealType sourceMealType,
		SlotRecipeTitle sourceRecipeTitle,
		MealType mealType = MealType.Dinner,
		SlotServings? servings = null)
	{
		var slot = FindSlot(day, mealType);
		slot.SetAsLeftover(sourceDay, sourceMealType, sourceRecipeTitle, servings);

		return this;
	}

	public WeeklyPlan ClearSlot(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.ClearSlot();

		return this;
	}

	public WeeklyPlan AdjustServings(DayOfWeek day, SlotServings servings, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.AdjustServingsTo(servings);

		return this;
	}

	public WeeklyPlan MarkAsCooked(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.MarkAsCooked();

		return this;
	}

	public WeeklyPlan MarkAsSkipped(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.MarkAsSkipped();

		return this;
	}

	public WeeklyPlan ResetToPlanned(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		var slot = FindSlot(day, mealType);
		slot.ResetToPlanned();

		return this;
	}

	/// <summary>
	/// Get meals that need cooked confirmation (planned recipes from past days).
	/// </summary>
	/// <param name="today">Today's day of week.</param>
	/// <returns>Slots that are Recipe type, still in Planned state, and before today.</returns>
	public IReadOnlyList<MealSlot> GetUnconfirmedPastMeals(DayOfWeek today)
	{
		return slots
			.Where(s => s.ContentType == SlotContentType.Recipe && s.State == MealState.Planned && s.Day < today)
			.ToList();
	}

	public WeeklyPlan SwapSlots(DayOfWeek day1, DayOfWeek day2, MealType mealType = MealType.Dinner)
	{
		var slot1 = FindSlot(day1, mealType);
		var slot2 = FindSlot(day2, mealType);

		var snapshot1 = slot1.TakeSnapshot();
		slot1.RestoreFromSnapshot(slot2.TakeSnapshot());
		slot2.RestoreFromSnapshot(snapshot1);

		return this;
	}

	private MealSlot FindSlot(DayOfWeek day, MealType mealType)
	{
		return slots.FirstOrDefault(s => s.Day == day && s.MealType == mealType)
			?? throw new EntityNotFoundException(nameof(MealSlot), $"{day}/{mealType}");
	}
}
#pragma warning restore SA1311
