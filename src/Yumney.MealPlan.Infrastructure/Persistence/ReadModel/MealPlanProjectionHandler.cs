using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler — rebuilds the meal plan read model from
/// integration events published by the event store.
/// </summary>
public sealed class MealPlanProjectionHandler(MealPlanReadDbContext context)
	: IModuleEventHandler<WeeklyPlanCreatedModuleEvent>,
	  IModuleEventHandler<ExtendedModeEnabledModuleEvent>,
	  IModuleEventHandler<ExtendedModeDisabledModuleEvent>,
	  IModuleEventHandler<RecipeAssignedModuleEvent>,
	  IModuleEventHandler<MealSetAsFreetextModuleEvent>,
	  IModuleEventHandler<LeftoverAssignedModuleEvent>,
	  IModuleEventHandler<MealSlotClearedModuleEvent>,
	  IModuleEventHandler<ServingsAdjustedModuleEvent>,
	  IModuleEventHandler<MealMarkedAsCookedModuleEvent>,
	  IModuleEventHandler<MealMarkedAsSkippedModuleEvent>,
	  IModuleEventHandler<MealResetToPlannedModuleEvent>,
	  IModuleEventHandler<MealSlotsSwappedModuleEvent>
{
#pragma warning disable SA1311
	private static readonly DayOfWeek[] allDays =
	[
		DayOfWeek.Monday,
		DayOfWeek.Tuesday,
		DayOfWeek.Wednesday,
		DayOfWeek.Thursday,
		DayOfWeek.Friday,
		DayOfWeek.Saturday,
		DayOfWeek.Sunday,
	];
#pragma warning restore SA1311

	public async Task HandleAsync(WeeklyPlanCreatedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var ownerId = @event.OwnerId;
		var week = @event.Week;
		var defaultServings = @event.Inner.DefaultServings.Value;

		var existing = await context.MealPlanWeekReadItems
			.AsTracking()
			.FirstOrDefaultAsync(w => w.OwnerId == ownerId && w.Week == week, cancellationToken);
		if (existing is null)
		{
			context.MealPlanWeekReadItems.Add(new MealPlanWeekReadItem
			{
				OwnerId = ownerId,
				Week = week,
				IsExtendedMode = false,
				LastUpdated = DateTime.UtcNow,
			});
			await context.SaveChangesAsync(cancellationToken);
		}

		foreach (var day in allDays)
		{
			await UpsertSlotAsync(ownerId, week, day.ToString(), nameof(MealType.Dinner), defaultServings, cancellationToken);
		}
	}

	public async Task HandleAsync(ExtendedModeEnabledModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var ownerId = @event.OwnerId;
		var week = @event.Week;
		var defaultServings = @event.Inner.DefaultServings.Value;

		var weekItem = await GetTrackedWeekAsync(ownerId, week, cancellationToken);
		if (weekItem is not null)
		{
			weekItem.IsExtendedMode = true;
			weekItem.LastUpdated = DateTime.UtcNow;
			await context.SaveChangesAsync(cancellationToken);
		}

		foreach (var day in allDays)
		{
			await UpsertSlotAsync(ownerId, week, day.ToString(), nameof(MealType.Breakfast), defaultServings, cancellationToken);
			await UpsertSlotAsync(ownerId, week, day.ToString(), nameof(MealType.Lunch), defaultServings, cancellationToken);
		}
	}

	public async Task HandleAsync(ExtendedModeDisabledModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var weekItem = await GetTrackedWeekAsync(@event.OwnerId, @event.Week, cancellationToken);
		if (weekItem is null) return;

		weekItem.IsExtendedMode = false;
		weekItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(RecipeAssignedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.ContentType = nameof(SlotContentType.Recipe);
		slot.RecipeIdentifier = inner.Recipe.RecipeIdentifier.Value;
		slot.RecipeTitle = inner.Recipe.Title.Value;
		slot.FreetextLabel = null;
		slot.LeftoverLabel = null;
		slot.LeftoverSourceDay = null;
		slot.LeftoverSourceMealType = null;
		if (inner.Servings is not null)
		{
			slot.Servings = inner.Servings.Value;
		}

		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealSetAsFreetextModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.ContentType = nameof(SlotContentType.Freetext);
		slot.FreetextLabel = inner.Label.Value;
		slot.RecipeIdentifier = null;
		slot.RecipeTitle = null;
		slot.LeftoverLabel = null;
		slot.LeftoverSourceDay = null;
		slot.LeftoverSourceMealType = null;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(LeftoverAssignedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.ContentType = nameof(SlotContentType.Leftover);
		slot.LeftoverLabel = LeftoverLabel.ForRecipe(inner.SourceRecipeTitle).Value;
		slot.LeftoverSourceDay = inner.SourceDay.ToString();
		slot.LeftoverSourceMealType = inner.SourceMealType.ToString();
		slot.RecipeIdentifier = null;
		slot.RecipeTitle = null;
		slot.FreetextLabel = null;
		if (inner.Servings is not null)
		{
			slot.Servings = inner.Servings.Value;
		}

		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealSlotClearedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.ContentType = nameof(SlotContentType.Empty);
		slot.RecipeIdentifier = null;
		slot.RecipeTitle = null;
		slot.FreetextLabel = null;
		slot.LeftoverLabel = null;
		slot.LeftoverSourceDay = null;
		slot.LeftoverSourceMealType = null;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ServingsAdjustedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.Servings = inner.Servings.Value;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealMarkedAsCookedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.State = nameof(MealState.Cooked);
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealMarkedAsSkippedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.State = nameof(MealState.Skipped);
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealResetToPlannedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);

		slot.State = nameof(MealState.Planned);
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealSlotsSwappedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot1 = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day1, inner.MealType, cancellationToken);
		var slot2 = await GetOrCreateTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day2, inner.MealType, cancellationToken);

		(slot1.ContentType, slot2.ContentType) = (slot2.ContentType, slot1.ContentType);
		(slot1.RecipeIdentifier, slot2.RecipeIdentifier) = (slot2.RecipeIdentifier, slot1.RecipeIdentifier);
		(slot1.RecipeTitle, slot2.RecipeTitle) = (slot2.RecipeTitle, slot1.RecipeTitle);
		(slot1.Servings, slot2.Servings) = (slot2.Servings, slot1.Servings);
		(slot1.FreetextLabel, slot2.FreetextLabel) = (slot2.FreetextLabel, slot1.FreetextLabel);
		(slot1.LeftoverLabel, slot2.LeftoverLabel) = (slot2.LeftoverLabel, slot1.LeftoverLabel);
		(slot1.LeftoverSourceDay, slot2.LeftoverSourceDay) = (slot2.LeftoverSourceDay, slot1.LeftoverSourceDay);
		(slot1.LeftoverSourceMealType, slot2.LeftoverSourceMealType) = (slot2.LeftoverSourceMealType, slot1.LeftoverSourceMealType);
		(slot1.State, slot2.State) = (slot2.State, slot1.State);

		slot1.LastUpdated = DateTime.UtcNow;
		slot2.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	// PostgreSQL atomic upsert. Idempotent and concurrency-safe — multiple
	// projection handlers seeding overlapping slot rows produce one row each
	// without throwing. Used both by the week-seeding handlers
	// (WeeklyPlanCreated, ExtendedModeEnabled) and by the on-demand
	// materialisation in GetOrCreateTrackedSlotAsync.
	private Task<int> UpsertSlotAsync(string ownerId, string week, string day, string mealType, int defaultServings, CancellationToken cancellationToken)
	{
		var newId = Guid.CreateVersion7();
		var emptyContent = SlotContentType.Empty.ToString();
		var plannedState = MealState.Planned.ToString();
		var now = DateTime.UtcNow;

		return context.Database.ExecuteSqlInterpolatedAsync(
			$@"INSERT INTO ""MealPlanSlotReadItems""
				(""Id"", ""OwnerId"", ""Week"", ""Day"", ""MealType"", ""ContentType"", ""Servings"", ""State"", ""LastUpdated"")
			   VALUES ({newId}, {ownerId}, {week}, {day}, {mealType}, {emptyContent}, {defaultServings}, {plannedState}, {now})
			   ON CONFLICT (""OwnerId"", ""Week"", ""Day"", ""MealType"") DO NOTHING",
			cancellationToken);
	}

	private Task<MealPlanWeekReadItem?> GetTrackedWeekAsync(string ownerId, string week, CancellationToken cancellationToken) =>
		context.MealPlanWeekReadItems
			.AsTracking()
			.FirstOrDefaultAsync(w => w.OwnerId == ownerId && w.Week == week, cancellationToken);

	private Task<MealPlanSlotReadItem?> GetTrackedSlotAsync(string ownerId, string week, DayOfWeek day, MealType mealType, CancellationToken cancellationToken)
	{
		var dayName = day.ToString();
		var mealTypeName = mealType.ToString();
		return context.MealPlanSlotReadItems
			.AsTracking()
			.FirstOrDefaultAsync(
				s => s.OwnerId == ownerId && s.Week == week && s.Day == dayName && s.MealType == mealTypeName,
				cancellationToken);
	}

	// Slot-mutation events (RecipeAssigned, MealSetAsFreetext, MarkedAsCooked,
	// etc.) can race ahead of the WeeklyPlanCreated event that seeds the
	// week's slot rows — Wolverine's local in-process bus dispatches the two
	// concurrently, and the slot writer can run before the seeder commits.
	// Falling back to a silent no-op (the previous behaviour) silently dropped
	// the user's update from the read model.
	//
	// We materialise the row through a PostgreSQL upsert: atomic INSERT … ON
	// CONFLICT DO NOTHING gives us a guaranteed-present row without throwing
	// duplicate-key exceptions, which would otherwise abort the WeeklyPlanCreated
	// handler's batched 7-slot insert and dead-letter that message.
	private async Task<MealPlanSlotReadItem> GetOrCreateTrackedSlotAsync(
		string ownerId,
		string week,
		DayOfWeek day,
		MealType mealType,
		CancellationToken cancellationToken)
	{
		var existing = await GetTrackedSlotAsync(ownerId, week, day, mealType, cancellationToken);
		if (existing is not null) return existing;

		await UpsertSlotAsync(ownerId, week, day.ToString(), mealType.ToString(), SlotServings.DefaultValue, cancellationToken);

		var slot = await GetTrackedSlotAsync(ownerId, week, day, mealType, cancellationToken);
		return slot ?? throw new InvalidOperationException(
			$"Slot upsert succeeded but no row is visible for {ownerId}/{week}/{day}/{mealType}.");
	}
}
