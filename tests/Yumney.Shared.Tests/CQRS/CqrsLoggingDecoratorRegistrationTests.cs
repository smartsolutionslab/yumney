using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class CqrsLoggingDecoratorRegistrationTests
{
    [Fact]
    public void AddCqrsLoggingDecorators_WrapsCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();

        services.AddCqrsLoggingDecorators();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<FakeCommand, string>>();
        handler.Should().BeOfType<LoggingCommandHandlerDecorator<FakeCommand, string>>();
    }

    [Fact]
    public void AddCqrsLoggingDecorators_WrapsQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddHandlersFromAssemblyContaining<FakeQueryHandler>();

        services.AddCqrsLoggingDecorators();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<FakeQuery, int>>();
        handler.Should().BeOfType<LoggingQueryHandlerDecorator<FakeQuery, int>>();
    }

    [Fact]
    public async Task AddCqrsLoggingDecorators_DecoratedHandlerStillDelegates()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();
        services.AddCqrsLoggingDecorators();
        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<FakeCommand, string>>();
        var result = await handler.HandleAsync(new FakeCommand());

        result.Should().Be("handled");
    }

    [Fact]
    public void AddCqrsLoggingDecorators_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddCqrsLoggingDecorators();

        result.Should().BeSameAs(services);
    }
}
