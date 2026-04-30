using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class EfCoreMealPlanEventStore(
	MealPlanDbContext context,
	IEventBus eventBus,
	ILogger<EfCoreMealPlanEventStore> logger) : IMealPlanEventStore
{
#pragma warning disable SA1311, SA1303, SA1204
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new JsonStringEnumConverter(),
			new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From),
			new StringValueObjectJsonConverter<FreetextLabel>(FreetextLabel.From),
			new StringValueObjectJsonConverter<SlotRecipeTitle>(SlotRecipeTitle.From),
			new IntValueObjectJsonConverter<SlotServings>(SlotServings.From),
			new WeekIdentifierJsonConverter(),
			new SlotRecipeReferenceJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> eventTypeMap = new()
	{
		[nameof(WeeklyPlanCreated)] = typeof(WeeklyPlanCreated),
		[nameof(ExtendedModeEnabled)] = typeof(ExtendedModeEnabled),
		[nameof(ExtendedModeDisabled)] = typeof(ExtendedModeDisabled),
		[nameof(RecipeAssigned)] = typeof(RecipeAssigned),
		[nameof(MealSetAsFreetext)] = typeof(MealSetAsFreetext),
		[nameof(LeftoverAssigned)] = typeof(LeftoverAssigned),
		[nameof(MealSlotCleared)] = typeof(MealSlotCleared),
		[nameof(ServingsAdjusted)] = typeof(ServingsAdjusted),
		[nameof(MealMarkedAsCooked)] = typeof(MealMarkedAsCooked),
		[nameof(MealMarkedAsSkipped)] = typeof(MealMarkedAsSkipped),
		[nameof(MealResetToPlanned)] = typeof(MealResetToPlanned),
		[nameof(MealSlotsSwapped)] = typeof(MealSlotsSwapped),
	};
#pragma warning restore SA1311

	public async Task<WeeklyPlan?> LoadAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;
		var weekValue = week.Value;

		var metadata = await context.Set<AggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.OwnerId == ownerValue && m.Week == weekValue, cancellationToken);

		if (metadata is null) return null;

		var aggregateId = metadata.AggregateId;

		var storedEvents = await context.Set<StoredEvent>()
			.AsNoTracking()
			.Where(stored => stored.AggregateId == aggregateId)
			.OrderBy(stored => stored.Version)
			.ToListAsync(cancellationToken);

		var events = storedEvents.Select(DeserializeEvent).Where(deserialized => deserialized is not null).Cast<IDomainEvent>();

		return WeeklyPlan.FromEvents(WeeklyPlanIdentifier.From(aggregateId), events);
	}

	public async Task SaveAsync(WeeklyPlan plan, CancellationToken cancellationToken = default)
	{
		var uncommitted = plan.UncommittedEvents.ToList();
		if (uncommitted.Count == 0) return;

		var existingMetadata = await context.Set<AggregateMetadata>()
			.FirstOrDefaultAsync(m => m.AggregateId == plan.Identifier.Value, cancellationToken);

		if (existingMetadata is null)
		{
			context.Set<AggregateMetadata>().Add(new AggregateMetadata
			{
				AggregateId = plan.Identifier.Value,
				OwnerId = plan.Owner.Value,
				Week = plan.Week.Value,
			});
		}

		var baseVersion = plan.Version - uncommitted.Count;

		for (var index = 0; index < uncommitted.Count; index++)
		{
			var @event = uncommitted[index];
			context.Set<StoredEvent>().Add(new StoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = plan.Identifier.Value,
				EventType = @event.GetType().Name,
				EventData = JsonSerializer.Serialize(@event, @event.GetType(), jsonOptions),
				Version = baseVersion + index + 1,
				OccurredAt = @event.OccurredOn,
			});
		}

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(nameof(WeeklyPlan), plan.Identifier.Value, ex);
		}

		plan.MarkCommitted();

		await PublishEventsAsync(plan.Owner.Value, plan.Week.Value, uncommitted, cancellationToken);

		LogEventsSaved(plan.Owner.Value, plan.Week.Value, uncommitted.Count, plan.Version);
	}

	private async Task PublishEventsAsync(string ownerId, string week, List<IDomainEvent> events, CancellationToken cancellationToken)
	{
		foreach (var @event in events)
		{
			IIntegrationEvent? integrationEvent = @event switch
			{
				WeeklyPlanCreated created => new WeeklyPlanCreatedIntegrationEvent(ownerId, week, created),
				ExtendedModeEnabled enabled => new ExtendedModeEnabledIntegrationEvent(ownerId, week, enabled),
				ExtendedModeDisabled disabled => new ExtendedModeDisabledIntegrationEvent(ownerId, week, disabled),
				RecipeAssigned assigned => new RecipeAssignedIntegrationEvent(ownerId, week, assigned),
				MealSetAsFreetext freetext => new MealSetAsFreetextIntegrationEvent(ownerId, week, freetext),
				LeftoverAssigned leftover => new LeftoverAssignedIntegrationEvent(ownerId, week, leftover),
				MealSlotCleared cleared => new MealSlotClearedIntegrationEvent(ownerId, week, cleared),
				ServingsAdjusted adjusted => new ServingsAdjustedIntegrationEvent(ownerId, week, adjusted),
				MealMarkedAsCooked cooked => new MealMarkedAsCookedIntegrationEvent(ownerId, week, cooked),
				MealMarkedAsSkipped skipped => new MealMarkedAsSkippedIntegrationEvent(ownerId, week, skipped),
				MealResetToPlanned reset => new MealResetToPlannedIntegrationEvent(ownerId, week, reset),
				MealSlotsSwapped swapped => new MealSlotsSwappedIntegrationEvent(ownerId, week, swapped),
				_ => null,
			};

			if (integrationEvent is not null)
			{
				await PublishAsync(integrationEvent, cancellationToken);
			}

			if (@event is MealMarkedAsCooked confirmed && confirmed.Recipe is not null)
			{
				var crossModule = new MealConfirmedIntegrationEvent(
					ownerId,
					confirmed.Recipe.RecipeIdentifier.Value,
					confirmed.Servings.Value,
					confirmed.Ingredients
						.Select(ingredient => new MealConfirmedIngredient(ingredient.Name, ingredient.Quantity, ingredient.Unit))
						.ToList());
				await PublishAsync(crossModule, cancellationToken);
			}
		}
	}

	private async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken)
		where T : class, IIntegrationEvent
	{
		await eventBus.PublishAsync(integrationEvent, cancellationToken);
	}

	private static IDomainEvent? DeserializeEvent(StoredEvent stored)
	{
		if (!eventTypeMap.TryGetValue(stored.EventType, out var type)) return null;

		return JsonSerializer.Deserialize(stored.EventData, type, jsonOptions) as IDomainEvent;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId} week {Week}, version now {Version}")]
	private partial void LogEventsSaved(string ownerId, string week, int count, int version);
}
