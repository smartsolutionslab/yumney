using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Events;

public class IntegrationEventHandlerRegistrationTests
{
	[Fact]
	public void AddIntegrationEventHandlersFromAssemblyContaining_RegistersConcreteAsScoped()
	{
		ServiceCollection services = [];

		services.AddIntegrationEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var concrete = scope.ServiceProvider.GetRequiredService<MultiEventConsumer>();
		concrete.Should().NotBeNull();
	}

	[Fact]
	public void AddIntegrationEventHandlersFromAssemblyContaining_PointsEveryInterfaceAtTheSameInstance()
	{
		ServiceCollection services = [];

		services.AddIntegrationEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var first = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();
		var second = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<SecondEvent>>();

		first.Should().BeSameAs(second, "a handler that consumes multiple events must share one instance per scope");
	}

	[Fact]
	public void AddIntegrationEventHandlersFromAssemblyContaining_DifferentScopes_GetDifferentInstances()
	{
		ServiceCollection services = [];

		services.AddIntegrationEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();

		var firstHandler = firstScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();
		var secondHandler = secondScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();

		firstHandler.Should().NotBeSameAs(secondHandler);
	}

	[Fact]
	public void AddIntegrationEventHandlersFromAssemblyContaining_RegistersSingleEventConsumer()
	{
		ServiceCollection services = [];

		services.AddIntegrationEventHandlersFromAssemblyContaining<SingleEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ThirdEvent>>();
		handler.Should().BeOfType<SingleEventConsumer>();
	}
}

public sealed record FirstEvent : IntegrationEvent;

public sealed record SecondEvent : IntegrationEvent;

public sealed record ThirdEvent : IntegrationEvent;

public sealed class MultiEventConsumer
	: IIntegrationEventHandler<FirstEvent>, IIntegrationEventHandler<SecondEvent>
{
	public Task HandleAsync(FirstEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;

	public Task HandleAsync(SecondEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class SingleEventConsumer : IIntegrationEventHandler<ThirdEvent>
{
	public Task HandleAsync(ThirdEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
