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
	: IIntegrationEventHandler<WeeklyPlanCreatedIntegrationEvent>,
	  IIntegrationEventHandler<ExtendedModeEnabledIntegrationEvent>,
	  IIntegrationEventHandler<ExtendedModeDisabledIntegrationEvent>,
	  IIntegrationEventHandler<RecipeAssignedIntegrationEvent>,
	  IIntegrationEventHandler<MealSetAsFreetextIntegrationEvent>,
	  IIntegrationEventHandler<LeftoverAssignedIntegrationEvent>,
	  IIntegrationEventHandler<MealSlotClearedIntegrationEvent>,
	  IIntegrationEventHandler<ServingsAdjustedIntegrationEvent>,
	  IIntegrationEventHandler<MealMarkedAsCookedIntegrationEvent>,
	  IIntegrationEventHandler<MealMarkedAsSkippedIntegrationEvent>,
	  IIntegrationEventHandler<MealResetToPlannedIntegrationEvent>,
	  IIntegrationEventHandler<MealSlotsSwappedIntegrationEvent>
{
#pragma warning disable SA1311
	private static readonly DayOfWeek[] allDays =
	[
		DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
		DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
	];
#pragma warning restore SA1311

	public async Task HandleAsync(WeeklyPlanCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
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
		}

		var existingKeys = await LoadExistingSlotKeysAsync(ownerId, week, cancellationToken);
		foreach (var day in allDays)
		{
			AddSlotIfMissing(existingKeys, ownerId, week, day.ToString(), MealType.Dinner.ToString(), defaultServings);
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ExtendedModeEnabledIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var ownerId = @event.OwnerId;
		var week = @event.Week;
		var defaultServings = @event.Inner.DefaultServings.Value;

		var weekItem = await GetTrackedWeekAsync(ownerId, week, cancellationToken);
		if (weekItem is not null)
		{
			weekItem.IsExtendedMode = true;
			weekItem.LastUpdated = DateTime.UtcNow;
		}

		var existingKeys = await LoadExistingSlotKeysAsync(ownerId, week, cancellationToken);
		foreach (var day in allDays)
		{
			AddSlotIfMissing(existingKeys, ownerId, week, day.ToString(), MealType.Breakfast.ToString(), defaultServings);
			AddSlotIfMissing(existingKeys, ownerId, week, day.ToString(), MealType.Lunch.ToString(), defaultServings);
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ExtendedModeDisabledIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var weekItem = await GetTrackedWeekAsync(@event.OwnerId, @event.Week, cancellationToken);
		if (weekItem is null) return;

		weekItem.IsExtendedMode = false;
		weekItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(RecipeAssignedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.ContentType = SlotContentType.Recipe.ToString();
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

	public async Task HandleAsync(MealSetAsFreetextIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.ContentType = SlotContentType.Freetext.ToString();
		slot.FreetextLabel = inner.Label.Value;
		slot.RecipeIdentifier = null;
		slot.RecipeTitle = null;
		slot.LeftoverLabel = null;
		slot.LeftoverSourceDay = null;
		slot.LeftoverSourceMealType = null;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(LeftoverAssignedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.ContentType = SlotContentType.Leftover.ToString();
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

	public async Task HandleAsync(MealSlotClearedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.ContentType = SlotContentType.Empty.ToString();
		slot.RecipeIdentifier = null;
		slot.RecipeTitle = null;
		slot.FreetextLabel = null;
		slot.LeftoverLabel = null;
		slot.LeftoverSourceDay = null;
		slot.LeftoverSourceMealType = null;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ServingsAdjustedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.Servings = inner.Servings.Value;
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealMarkedAsCookedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.State = MealState.Cooked.ToString();
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealMarkedAsSkippedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.State = MealState.Skipped.ToString();
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealResetToPlannedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day, inner.MealType, cancellationToken);
		if (slot is null) return;

		slot.State = MealState.Planned.ToString();
		slot.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(MealSlotsSwappedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var slot1 = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day1, inner.MealType, cancellationToken);
		var slot2 = await GetTrackedSlotAsync(@event.OwnerId, @event.Week, inner.Day2, inner.MealType, cancellationToken);
		if (slot1 is null || slot2 is null) return;

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

	private async Task<HashSet<(string Day, string MealType)>> LoadExistingSlotKeysAsync(string ownerId, string week, CancellationToken cancellationToken)
	{
		var existing = await context.MealPlanSlotReadItems
			.Where(s => s.OwnerId == ownerId && s.Week == week)
			.Select(s => new { s.Day, s.MealType })
			.ToListAsync(cancellationToken);
		return existing.Select(s => (s.Day, s.MealType)).ToHashSet();
	}

	private void AddSlotIfMissing(HashSet<(string Day, string MealType)> existingKeys, string ownerId, string week, string day, string mealType, int defaultServings)
	{
		if (!existingKeys.Add((day, mealType))) return;

		context.MealPlanSlotReadItems.Add(new MealPlanSlotReadItem
		{
			Id = Guid.CreateVersion7(),
			OwnerId = ownerId,
			Week = week,
			Day = day,
			MealType = mealType,
			ContentType = SlotContentType.Empty.ToString(),
			Servings = defaultServings,
			State = MealState.Planned.ToString(),
			LastUpdated = DateTime.UtcNow,
		});
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
}
