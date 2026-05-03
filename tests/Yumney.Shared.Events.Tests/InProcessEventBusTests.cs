using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class InProcessEventBusTests
{
	[Fact]
	public async Task PublishAsync_WithRegisteredHandler_InvokesHandler()
	{
		// Arrange
		var handler = new TestIntegrationEventHandler();
		var eventBus = CreateEventBus(services => services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(handler));

		var integrationEvent = new TestIntegrationEvent();

		// Act
		await eventBus.PublishAsync(integrationEvent);

		// Assert
		handler.HandledEvents.Should().ContainSingle()
			.Which.Should().Be(integrationEvent);
	}

	[Fact]
	public async Task PublishAsync_WithMultipleHandlers_InvokesAll()
	{
		// Arrange
		var firstHandler = new TestIntegrationEventHandler();
		var secondHandler = new TestIntegrationEventHandler();
		var eventBus = CreateEventBus(services =>
		{
			services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(firstHandler);
			services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(secondHandler);
		});

		var integrationEvent = new TestIntegrationEvent();

		// Act
		await eventBus.PublishAsync(integrationEvent);

		// Assert
		firstHandler.HandledEvents.Should().ContainSingle();
		secondHandler.HandledEvents.Should().ContainSingle();
	}

	[Fact]
	public async Task PublishAsync_ViaInterfaceStaticType_StillReachesConcreteHandler()
	{
		// Regression: callers commonly map domain events to integration events with a
		// `IIntegrationEvent? = switch { ... }` expression — the variable's static type
		// is the marker interface, so naive generic-parameter dispatch would bind
		// TEvent to IIntegrationEvent and miss every concrete-type subscriber. The bus
		// must resolve handlers by the event's runtime type to avoid silently dropping
		// every integration event published this way.
		var handler = new TestIntegrationEventHandler();
		var eventBus = CreateEventBus(services => services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(handler));
		IIntegrationEvent integrationEvent = new TestIntegrationEvent();

		await eventBus.PublishAsync(integrationEvent);

		handler.HandledEvents.Should().ContainSingle().Which.Should().Be(integrationEvent);
	}

	[Fact]
	public async Task PublishAsync_WithNoHandler_CompletesWithoutError()
	{
		// Arrange
		var eventBus = CreateEventBus(_ => { });
		var integrationEvent = new TestIntegrationEvent();

		// Act
		var act = () => eventBus.PublishAsync(integrationEvent);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task PublishAsync_ModuleEvent_InvokesModuleEventHandler()
	{
		var handler = new TestModuleEventHandler();
		var eventBus = CreateEventBus(services => services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handler));
		var moduleEvent = new TestModuleEvent("owner-1");

		await eventBus.PublishAsync(moduleEvent);

		handler.HandledEvents.Should().ContainSingle().Which.Should().Be(moduleEvent);
	}

	[Fact]
	public async Task PublishAsync_ModuleEvent_DoesNotInvokeIntegrationEventHandler()
	{
		// Same generic event-type name space, but different handler interfaces.
		// A module event MUST NOT trigger an IIntegrationEventHandler<T> registration
		// (the bus branches by marker interface — this guards that branching).
		var integrationHandler = new TestIntegrationEventHandler();
		var moduleHandler = new TestModuleEventHandler();
		var eventBus = CreateEventBus(services =>
		{
			services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(integrationHandler);
			services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(moduleHandler);
		});

		await eventBus.PublishAsync(new TestModuleEvent("owner-1"));

		integrationHandler.HandledEvents.Should().BeEmpty();
		moduleHandler.HandledEvents.Should().ContainSingle();
	}

	[Fact]
	public async Task PublishAsync_ViaIBusEventStaticType_StillReachesConcreteModuleHandler()
	{
		// Same regression as PublishAsync_ViaInterfaceStaticType_StillReachesConcreteHandler,
		// but for the module-event branch: when callers map domain events to module events
		// via a `IModuleEvent? = switch { ... }` expression (e.g. EfCoreMealPlanEventStore),
		// the bus must still resolve handlers by the event's runtime concrete type.
		var handler = new TestModuleEventHandler();
		var eventBus = CreateEventBus(services => services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handler));
		IModuleEvent moduleEvent = new TestModuleEvent("owner-1");

		await eventBus.PublishAsync(moduleEvent);

		handler.HandledEvents.Should().ContainSingle().Which.Should().Be(moduleEvent);
	}

	private static InProcessEventBus CreateEventBus(Action<IServiceCollection> configureServices)
	{
		var services = new ServiceCollection();
		configureServices(services);
		var serviceProvider = services.BuildServiceProvider();

		return new InProcessEventBus(serviceProvider, NullLogger<InProcessEventBus>.Instance);
	}

	private sealed record TestIntegrationEvent : IntegrationEvent;

	private sealed record TestModuleEvent(string OwnerId) : ModuleEvent(OwnerId);

	private sealed class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
	{
		public List<TestIntegrationEvent> HandledEvents { get; } = [];

		public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
		{
			HandledEvents.Add(integrationEvent);
			return Task.CompletedTask;
		}
	}

	private sealed class TestModuleEventHandler : IModuleEventHandler<TestModuleEvent>
	{
		public List<TestModuleEvent> HandledEvents { get; } = [];

		public Task HandleAsync(TestModuleEvent moduleEvent, CancellationToken cancellationToken = default)
		{
			HandledEvents.Add(moduleEvent);
			return Task.CompletedTask;
		}
	}
}
