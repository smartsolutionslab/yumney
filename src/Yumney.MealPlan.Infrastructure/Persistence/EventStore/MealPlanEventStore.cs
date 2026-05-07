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
	private static readonly IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> moduleEventFactories =
		ModuleEventConvention.BuildMap(typeof(MealPlanModuleEvent).Assembly, typeof(string), typeof(string));

	private static readonly IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> crossModuleEventFactories =
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

	protected override IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> ModuleEventFactories => moduleEventFactories;

	protected override IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> CrossModuleEventFactories => crossModuleEventFactories;

	protected override object[] BuildEventContext(WeeklyPlan aggregate) => [aggregate.Owner.Value, aggregate.Week.Value];

	protected override void LogEventsSaved(WeeklyPlan aggregate, IReadOnlyList<IDomainEvent> events) =>
		LogEventsSavedCore(aggregate.Owner.Value, aggregate.Week.Value, events.Count, aggregate.Version);

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId} week {Week}, version now {Version}")]
	private partial void LogEventsSavedCore(string ownerId, string week, int count, int version);
}
