using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Yumney.Shared.Common;
using Yumney.Shared.Events;

namespace Yumney.Shared.Tests.Events;

public class InProcessDomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithRegisteredHandler_InvokesHandler()
    {
        // Arrange
        var handler = new TestDomainEventHandler();
        var dispatcher = CreateDispatcher(services => services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(handler));

        var domainEvent = new TestDomainEvent();

        // Act
        await dispatcher.DispatchAsync([domainEvent]);

        // Assert
        handler.HandledEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_InvokesAll()
    {
        // Arrange
        var firstHandler = new TestDomainEventHandler();
        var secondHandler = new TestDomainEventHandler();
        var dispatcher = CreateDispatcher(services =>
        {
            services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(firstHandler);
            services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(secondHandler);
        });

        var domainEvent = new TestDomainEvent();

        // Act
        await dispatcher.DispatchAsync([domainEvent]);

        // Assert
        firstHandler.HandledEvents.Should().ContainSingle();
        secondHandler.HandledEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandler_CompletesWithoutError()
    {
        // Arrange
        var dispatcher = CreateDispatcher(_ => { });
        var domainEvent = new TestDomainEvent();

        // Act
        var act = () => dispatcher.DispatchAsync([domainEvent]);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleEvents_HandlesAll()
    {
        // Arrange
        var handler = new TestDomainEventHandler();
        var dispatcher = CreateDispatcher(services =>
            services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(handler));

        var firstEvent = new TestDomainEvent();
        var secondEvent = new TestDomainEvent();

        // Act
        await dispatcher.DispatchAsync([firstEvent, secondEvent]);

        // Assert
        handler.HandledEvents.Should().HaveCount(2);
        handler.HandledEvents.Should().Contain(firstEvent);
        handler.HandledEvents.Should().Contain(secondEvent);
    }

    private static InProcessDomainEventDispatcher CreateDispatcher(
        Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        return new InProcessDomainEventDispatcher(serviceProvider, NullLogger<InProcessDomainEventDispatcher>.Instance);
    }

    private sealed record TestDomainEvent : DomainEvent;

    private sealed class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
    {
        public List<TestDomainEvent> HandledEvents { get; } = [];

        public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
