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

	private static InProcessEventBus CreateEventBus(Action<IServiceCollection> configureServices)
	{
		var services = new ServiceCollection();
		configureServices(services);
		var serviceProvider = services.BuildServiceProvider();

		return new InProcessEventBus(serviceProvider, NullLogger<InProcessEventBus>.Instance);
	}

	private sealed record TestIntegrationEvent : IntegrationEvent;

	private sealed class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
	{
		public List<TestIntegrationEvent> HandledEvents { get; } = [];

		public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
		{
			HandledEvents.Add(integrationEvent);
			return Task.CompletedTask;
		}
	}
}
