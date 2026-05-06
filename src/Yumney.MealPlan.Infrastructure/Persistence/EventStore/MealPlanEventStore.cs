using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class MealPlanEventStore(MealPlanDbContext context, IEventBus eventBus, ILogger<MealPlanEventStore> logger)
	: EfCoreEventStoreBase<WeeklyPlan, WeeklyPlanIdentifier, AggregateMetadata, StoredEvent>(
		context,
		eventBus,
		MealPlanEventSerializer.Instance,
		logger),
	IMealPlanEventStore
{
#pragma warning disable SA1311
	private static readonly IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> moduleEventWrappers =
		ModuleEventConvention.BuildMap(typeof(MealPlanModuleEvent).Assembly, typeof(string), typeof(string));

	private static readonly IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> crossModuleMappers =
		CrossModuleEventConvention.BuildMap(typeof(MealPlanEventStore).Assembly);
#pragma warning restore SA1311

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

	protected override Guid GetAggregateId(WeeklyPlan aggregate) => aggregate.Identifier.Value;

	protected override AggregateMetadata BuildMetadata(WeeklyPlan aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier.Value,
			OwnerId = aggregate.Owner.Value,
			Week = aggregate.Week.Value,
		};

	protected override async Task PublishEventsAsync(WeeklyPlan aggregate, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
	{
		var ownerId = aggregate.Owner.Value;
		var week = aggregate.Week.Value;

		LogEventsSaved(ownerId, week, events.Count, aggregate.Version);

		object[] context = [ownerId, week];

		foreach (var @event in events)
		{
			if (moduleEventWrappers.TryGetValue(@event.GetType(), out var factory))
			{
				await EventBus.PublishAsync(factory(context, @event), cancellationToken);
			}

			if (crossModuleMappers.TryGetValue(@event.GetType(), out var crossFactory))
			{
				var crossEvent = crossFactory(context, @event);
				if (crossEvent is not null)
				{
					await EventBus.PublishAsync(crossEvent, cancellationToken);
				}
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId} week {Week}, version now {Version}")]
	private partial void LogEventsSaved(string ownerId, string week, int count, int version);
}
