using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class MealPlanEventStore(MealPlanDbContext context, IEventBus eventBus, ILogger<MealPlanEventStore> logger)
	: EfCoreEventStoreBase<WeeklyPlan, WeeklyPlanIdentifier, AggregateMetadata, StoredEvent>(
		context,
		eventBus,
		MealPlanEventSerializer.Instance),
	IMealPlanEventStore
{
	public async Task<WeeklyPlan> LoadAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
		=> await FindAsync(owner, week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");

	public async Task<WeeklyPlan?> FindAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;
		var weekValue = week.Value;

		var metadata = await Context.Set<AggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.OwnerId == ownerValue && m.Week == weekValue, cancellationToken);

		if (metadata is null) return null;

		var events = await LoadEventsAsync(metadata.AggregateId, cancellationToken);
		return WeeklyPlan.FromEvents(WeeklyPlanIdentifier.From(metadata.AggregateId), events);
	}

	protected override string AggregateName => nameof(WeeklyPlan);

	protected override Guid GetAggregateId(WeeklyPlan aggregate) => aggregate.Identifier.Value;

	protected override AggregateMetadata BuildMetadata(WeeklyPlan aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier.Value,
			OwnerId = aggregate.Owner.Value,
			Week = aggregate.Week.Value,
		};

	protected override async Task PublishEventsAsync(
		WeeklyPlan aggregate,
		IReadOnlyList<IDomainEvent> events,
		CancellationToken cancellationToken)
	{
		var ownerId = aggregate.Owner.Value;
		var week = aggregate.Week.Value;

		LogEventsSaved(ownerId, week, events.Count, aggregate.Version);

		foreach (var @event in events)
		{
			IModuleEvent? moduleEvent = @event switch
			{
				WeeklyPlanCreated created => new WeeklyPlanCreatedModuleEvent(ownerId, week, created),
				ExtendedModeEnabled enabled => new ExtendedModeEnabledModuleEvent(ownerId, week, enabled),
				ExtendedModeDisabled disabled => new ExtendedModeDisabledModuleEvent(ownerId, week, disabled),
				RecipeAssigned assigned => new RecipeAssignedModuleEvent(ownerId, week, assigned),
				MealSetAsFreetext freetext => new MealSetAsFreetextModuleEvent(ownerId, week, freetext),
				LeftoverAssigned leftover => new LeftoverAssignedModuleEvent(ownerId, week, leftover),
				MealSlotCleared cleared => new MealSlotClearedModuleEvent(ownerId, week, cleared),
				ServingsAdjusted adjusted => new ServingsAdjustedModuleEvent(ownerId, week, adjusted),
				MealMarkedAsCooked cooked => new MealMarkedAsCookedModuleEvent(ownerId, week, cooked),
				MealMarkedAsSkipped skipped => new MealMarkedAsSkippedModuleEvent(ownerId, week, skipped),
				MealResetToPlanned reset => new MealResetToPlannedModuleEvent(ownerId, week, reset),
				MealSlotsSwapped swapped => new MealSlotsSwappedModuleEvent(ownerId, week, swapped),
				_ => null,
			};

			if (moduleEvent is not null)
			{
				await EventBus.PublishAsync(moduleEvent, cancellationToken);
			}

			if (@event is MealMarkedAsCooked confirmed && confirmed.Recipe is not null)
			{
				var crossModule = new MealConfirmedIntegrationEvent(
					ownerId,
					confirmed.Recipe.RecipeIdentifier.Value,
					confirmed.Servings.Value,
					confirmed.Ingredients.Select(ingredient => new MealConfirmedIngredient(
							ingredient.Name,
							ingredient.Quantity,
							ingredient.Unit))
						.ToList());
				await EventBus.PublishAsync(crossModule, cancellationToken);
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId} week {Week}, version now {Version}")]
	private partial void LogEventsSaved(string ownerId, string week, int count, int version);
}
