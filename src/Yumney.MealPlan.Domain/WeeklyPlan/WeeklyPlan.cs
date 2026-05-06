using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

#pragma warning disable SA1311
public sealed class WeeklyPlan : EventSourcedAggregate<WeeklyPlanIdentifier>
{
	private static readonly DayOfWeek[] allDays = WeekDays.MondayToSunday;

	private readonly Dictionary<SlotKey, MealSlot> slots = [];
	private SlotServings defaultServings = SlotServings.Default();

	public OwnerIdentifier Owner { get; private set; } = default!;

	public WeekIdentifier Week { get; private set; } = default!;

	public bool IsExtendedMode { get; private set; }

	public IReadOnlyList<MealSlot> Slots =>
		slots.Values
			.OrderBy(slot => slot.Day)
			.ThenBy(slot => slot.MealType)
			.ToList()
			.AsReadOnly();

	private WeeklyPlan()
	{
		On<WeeklyPlanCreated>(OnCreated);
		On<ExtendedModeEnabled>(OnExtendedModeEnabled);
		On<ExtendedModeDisabled>(_ => OnExtendedModeDisabled());
		On<RecipeAssigned>(OnRecipeAssigned);
		On<MealSetAsFreetext>(OnMealSetAsFreetext);
		On<LeftoverAssigned>(OnLeftoverAssigned);
		On<MealSlotCleared>(OnMealSlotCleared);
		On<ServingsAdjusted>(OnServingsAdjusted);
		On<MealMarkedAsCooked>(OnMealMarkedAsCooked);
		On<MealMarkedAsSkipped>(OnMealMarkedAsSkipped);
		On<MealResetToPlanned>(OnMealResetToPlanned);
		On<MealSlotsSwapped>(OnMealSlotsSwapped);
	}

	public static WeeklyPlan Create(OwnerIdentifier owner, WeekIdentifier week, SlotServings? defaultServings = null)
	{
		var plan = new WeeklyPlan { Identifier = WeeklyPlanIdentifier.New() };
		plan.RaiseEvent(new WeeklyPlanCreated(owner, week, defaultServings ?? SlotServings.Default()));
		return plan;
	}

	public static WeeklyPlan FromEvents(
		WeeklyPlanIdentifier identifier,
		IEnumerable<IDomainEvent> events,
		AggregateVersion? startVersion = null)
	{
		var plan = new WeeklyPlan { Identifier = identifier };
		plan.LoadFromHistory(events, startVersion);
		return plan;
	}

	public IReadOnlyList<MealSlot> GetVisibleSlots()
	{
		return IsExtendedMode
			? Slots
			: slots.Values
				.Where(slot => slot.MealType == MealType.Dinner)
				.OrderBy(slot => slot.Day)
				.ToList()
				.AsReadOnly();
	}

	public IReadOnlyList<MealSlot> GetUnconfirmedPastMeals(DayOfWeek today)
	{
		return slots.Values
			.Where(slot => slot.ContentType == SlotContentType.Recipe && slot.State == MealState.Planned && slot.Day < today)
			.OrderBy(slot => slot.Day)
			.ThenBy(slot => slot.MealType)
			.ToList();
	}

	public WeeklyPlan EnableExtendedMode(SlotServings? overrideDefault = null)
	{
		if (IsExtendedMode) return this;
		RaiseEvent(new ExtendedModeEnabled(overrideDefault ?? defaultServings));
		return this;
	}

	public WeeklyPlan DisableExtendedMode()
	{
		if (!IsExtendedMode) return this;
		RaiseEvent(new ExtendedModeDisabled());
		return this;
	}

	public WeeklyPlan AssignRecipe(
		DayOfWeek day,
		SlotRecipeReference recipe,
		MealType mealType = MealType.Dinner,
		SlotServings? servings = null)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new RecipeAssigned(day, mealType, recipe, servings));
		return this;
	}

	public WeeklyPlan SetFreetext(DayOfWeek day, FreetextLabel label, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new MealSetAsFreetext(day, mealType, label));
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
		EnsureSlotExists(day, mealType);
		RaiseEvent(new LeftoverAssigned(day, mealType, sourceDay, sourceMealType, sourceRecipeTitle, servings));
		return this;
	}

	public WeeklyPlan ClearSlot(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new MealSlotCleared(day, mealType));
		return this;
	}

	public WeeklyPlan AdjustServings(DayOfWeek day, SlotServings servings, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new ServingsAdjusted(day, mealType, servings));
		return this;
	}

	public WeeklyPlan MarkAsCooked(
		DayOfWeek day,
		MealType mealType = MealType.Dinner,
		IReadOnlyList<CookedIngredient>? ingredients = null)
	{
		var slot = FindSlot(day, mealType);
		RaiseEvent(new MealMarkedAsCooked(day, mealType, slot.Recipe, slot.Servings, ingredients ?? []));
		return this;
	}

	public WeeklyPlan MarkAsSkipped(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new MealMarkedAsSkipped(day, mealType));
		return this;
	}

	public WeeklyPlan ResetToPlanned(DayOfWeek day, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day, mealType);
		RaiseEvent(new MealResetToPlanned(day, mealType));
		return this;
	}

	public WeeklyPlan SwapSlots(DayOfWeek day1, DayOfWeek day2, MealType mealType = MealType.Dinner)
	{
		EnsureSlotExists(day1, mealType);
		EnsureSlotExists(day2, mealType);
		RaiseEvent(new MealSlotsSwapped(day1, day2, mealType));
		return this;
	}

	private void OnCreated(WeeklyPlanCreated e)
	{
		Owner = e.Owner;
		Week = e.Week;
		defaultServings = e.DefaultServings;
		foreach (var day in allDays)
		{
			slots[new SlotKey(day, MealType.Dinner)] = MealSlot.Empty(day, MealType.Dinner, defaultServings);
		}
	}

	private void OnExtendedModeEnabled(ExtendedModeEnabled e)
	{
		foreach (var day in allDays)
		{
			var breakfastKey = new SlotKey(day, MealType.Breakfast);
			if (!slots.ContainsKey(breakfastKey))
			{
				slots[breakfastKey] = MealSlot.Empty(day, MealType.Breakfast, e.DefaultServings);
			}

			var lunchKey = new SlotKey(day, MealType.Lunch);
			if (!slots.ContainsKey(lunchKey))
			{
				slots[lunchKey] = MealSlot.Empty(day, MealType.Lunch, e.DefaultServings);
			}
		}

		IsExtendedMode = true;
	}

	private void OnExtendedModeDisabled() => IsExtendedMode = false;

	private void OnRecipeAssigned(RecipeAssigned e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].WithRecipe(e.Recipe, e.Servings);
	}

	private void OnMealSetAsFreetext(MealSetAsFreetext e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].WithFreetext(e.Label);
	}

	private void OnLeftoverAssigned(LeftoverAssigned e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].WithLeftover(e.SourceDay, e.SourceMealType, e.SourceRecipeTitle, e.Servings);
	}

	private void OnMealSlotCleared(MealSlotCleared e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].Cleared();
	}

	private void OnServingsAdjusted(ServingsAdjusted e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].WithServings(e.Servings);
	}

	private void OnMealMarkedAsCooked(MealMarkedAsCooked e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].InState(MealState.Cooked);
	}

	private void OnMealMarkedAsSkipped(MealMarkedAsSkipped e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].InState(MealState.Skipped);
	}

	private void OnMealResetToPlanned(MealResetToPlanned e)
	{
		var key = new SlotKey(e.Day, e.MealType);
		slots[key] = slots[key].InState(MealState.Planned);
	}

	private void OnMealSlotsSwapped(MealSlotsSwapped e)
	{
		var key1 = new SlotKey(e.Day1, e.MealType);
		var key2 = new SlotKey(e.Day2, e.MealType);
		var slot1 = slots[key1];
		var slot2 = slots[key2];
		slots[key1] = slot2 with { Day = e.Day1, MealType = e.MealType };
		slots[key2] = slot1 with { Day = e.Day2, MealType = e.MealType };
	}

	private void EnsureSlotExists(DayOfWeek day, MealType mealType)
	{
		if (!slots.ContainsKey(new SlotKey(day, mealType)))
		{
			throw new EntityNotFoundException(nameof(MealSlot), $"{day}/{mealType}");
		}
	}

	private MealSlot FindSlot(DayOfWeek day, MealType mealType)
	{
		var key = new SlotKey(day, mealType);
		if (!slots.TryGetValue(key, out var slot))
		{
			throw new EntityNotFoundException(nameof(MealSlot), $"{day}/{mealType}");
		}

		return slot;
	}

	private readonly record struct SlotKey(DayOfWeek Day, MealType MealType);
}
#pragma warning restore SA1311
