using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Events;

public class BusEventHandlerRegistrationTests
{
	[Fact]
	public void AddBusEventHandlersFromAssemblyContaining_RegistersConcreteAsScoped()
	{
		ServiceCollection services = [];

		services.AddBusEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var concrete = scope.ServiceProvider.GetRequiredService<MultiEventConsumer>();
		concrete.Should().NotBeNull();
	}

	[Fact]
	public void AddBusEventHandlersFromAssemblyContaining_PointsEveryInterfaceAtTheSameInstance()
	{
		ServiceCollection services = [];

		services.AddBusEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var first = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();
		var second = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<SecondEvent>>();

		first.Should().BeSameAs(second, "a handler that consumes multiple events must share one instance per scope");
	}

	[Fact]
	public void AddBusEventHandlersFromAssemblyContaining_DifferentScopes_GetDifferentInstances()
	{
		ServiceCollection services = [];

		services.AddBusEventHandlersFromAssemblyContaining<MultiEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();

		var firstHandler = firstScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();
		var secondHandler = secondScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<FirstEvent>>();

		firstHandler.Should().NotBeSameAs(secondHandler);
	}

	[Fact]
	public void AddBusEventHandlersFromAssemblyContaining_RegistersSingleEventConsumer()
	{
		ServiceCollection services = [];

		services.AddBusEventHandlersFromAssemblyContaining<SingleEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ThirdEvent>>();
		handler.Should().BeOfType<SingleEventConsumer>();
	}

	[Fact]
	public void AddBusEventHandlersFromAssemblyContaining_RegistersModuleEventHandler()
	{
		ServiceCollection services = [];

		services.AddBusEventHandlersFromAssemblyContaining<TestModuleEventConsumer>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var handler = scope.ServiceProvider.GetRequiredService<IModuleEventHandler<TestModuleEvent>>();
		handler.Should().BeOfType<TestModuleEventConsumer>();
	}
}

public sealed record FirstEvent : IntegrationEvent;

public sealed record SecondEvent : IntegrationEvent;

public sealed record ThirdEvent : IntegrationEvent;

public sealed record TestModuleEvent(string OwnerId) : ModuleEvent(OwnerId);

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

public sealed class TestModuleEventConsumer : IModuleEventHandler<TestModuleEvent>
{
	public Task HandleAsync(TestModuleEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
