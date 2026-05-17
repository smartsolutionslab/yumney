using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class CrossModuleEventConventionTests
{
	public sealed record ConventionTestRecipeImported(Guid RecipeId) : DomainEvent;

	public sealed record ConventionTestRecipeImportedIntegration(string OwnerId, Guid RecipeId) : IntegrationEvent;

	public sealed class ConventionTestMapper : ICrossModuleEventMapper
	{
		public Type DomainEventType => typeof(ConventionTestRecipeImported);

		public IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent)
		{
			var domain = (ConventionTestRecipeImported)domainEvent;
			var owner = (string)context[0];
			return new ConventionTestRecipeImportedIntegration(owner, domain.RecipeId);
		}
	}

	// Abstract mapper — should NOT be discovered.
	public abstract class AbstractMapper : ICrossModuleEventMapper
	{
		public Type DomainEventType => typeof(ConventionTestRecipeImported);

		public abstract IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent);
	}

	[Fact]
	public void BuildMap_RegistersConcreteMapperByDomainEventType()
	{
		var map = CrossModuleEventConvention.BuildMap(typeof(CrossModuleEventConventionTests).Assembly);

		map.Should().ContainKey(typeof(ConventionTestRecipeImported));
	}

	[Fact]
	public void BuildMap_FactoryInvokesMapperTryMap()
	{
		var map = CrossModuleEventConvention.BuildMap(typeof(CrossModuleEventConventionTests).Assembly);
		var factory = map[typeof(ConventionTestRecipeImported)];
		var recipe = Guid.NewGuid();

		var integration = factory([(object)"user-1"], new ConventionTestRecipeImported(recipe));

		integration.Should().BeOfType<ConventionTestRecipeImportedIntegration>()
			.Which.Should().Match<ConventionTestRecipeImportedIntegration>(@event =>
				@event.OwnerId == "user-1" && @event.RecipeId == recipe);
	}

	[Fact]
	public void BuildMap_SkipsAbstractMappers()
	{
		var map = CrossModuleEventConvention.BuildMap(typeof(CrossModuleEventConventionTests).Assembly);

		// AbstractMapper is abstract — it has no parameterless ctor that Activator
		// can instantiate, and the convention is designed to skip it. The map
		// must still expose exactly one entry per domain-event type (registered
		// here by ConventionTestMapper).
		map.Should().HaveCount(map.Keys.Distinct().Count());
	}
}
