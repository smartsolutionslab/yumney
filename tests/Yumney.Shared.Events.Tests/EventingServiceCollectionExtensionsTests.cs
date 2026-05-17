using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class EventingServiceCollectionExtensionsTests
{
	[Fact]
	public void AddInProcessDomainEventDispatcher_RegistersDispatcherAsScoped()
	{
		var services = new ServiceCollection();

		services.AddInProcessDomainEventDispatcher();

		var descriptor = services.Single(service => service.ServiceType == typeof(IDomainEventDispatcher));
		descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
		descriptor.ImplementationType.Should().Be<InProcessDomainEventDispatcher>();
	}

	[Fact]
	public void AddInProcessDomainEventDispatcher_RegistersEventMetricsAsSingleton()
	{
		var services = new ServiceCollection();

		services.AddInProcessDomainEventDispatcher();

		var descriptor = services.Single(service => service.ServiceType == typeof(EventMetrics));
		descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
	}

	[Fact]
	public void AddInProcessDomainEventDispatcher_ReturnsServiceCollectionForChaining()
	{
		var services = new ServiceCollection();

		var returned = services.AddInProcessDomainEventDispatcher();

		returned.Should().BeSameAs(services);
	}

	[Fact]
	public void AddInProcessEventBus_RegistersBusAsScoped()
	{
		var services = new ServiceCollection();

		services.AddInProcessEventBus();

		var descriptor = services.Single(service => service.ServiceType == typeof(IEventBus));
		descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
		descriptor.ImplementationType.Should().Be<InProcessEventBus>();
	}

	[Fact]
	public void AddInProcessEventBus_RegistersEventMetricsAsSingletonOnce()
	{
		var services = new ServiceCollection();

		services.AddInProcessEventBus();
		services.AddInProcessEventBus();

		services.Count(service => service.ServiceType == typeof(EventMetrics)).Should().Be(1);
	}

	[Fact]
	public void AddInProcessEventBus_ReturnsServiceCollectionForChaining()
	{
		var services = new ServiceCollection();

		var returned = services.AddInProcessEventBus();

		returned.Should().BeSameAs(services);
	}
}
