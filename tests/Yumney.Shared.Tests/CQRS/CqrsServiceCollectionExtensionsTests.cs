using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class CqrsServiceCollectionExtensionsTests
{
	[Fact]
	public void AddHandlersFromAssemblyContaining_RegistersCommandHandler()
	{
		var services = new ServiceCollection();

		services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();

		var provider = services.BuildServiceProvider();
		var handler = provider.GetService<ICommandHandler<FakeCommand, string>>();
		handler.Should().BeOfType<FakeCommandHandler>();
	}

	[Fact]
	public void AddHandlersFromAssemblyContaining_RegistersQueryHandler()
	{
		var services = new ServiceCollection();

		services.AddHandlersFromAssemblyContaining<FakeQueryHandler>();

		var provider = services.BuildServiceProvider();
		var handler = provider.GetService<IQueryHandler<FakeQuery, int>>();
		handler.Should().BeOfType<FakeQueryHandler>();
	}

	[Fact]
	public void AddHandlersFromAssemblyContaining_RegistersAsScoped()
	{
		var services = new ServiceCollection();

		services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();

		var descriptor = services.Should().ContainSingle(
			d => d.ServiceType == typeof(ICommandHandler<FakeCommand, string>)).Subject;
		descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
	}

	[Fact]
	public void AddHandlersFromAssemblyContaining_DoesNotRegisterAbstractHandlers()
	{
		var services = new ServiceCollection();

		services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();

		var provider = services.BuildServiceProvider();
		var handler = provider.GetService<ICommandHandler<AbstractCommand, string>>();
		handler.Should().BeNull();
	}

	[Fact]
	public void AddHandlersFromAssemblyContaining_ReturnsSameServiceCollection()
	{
		var services = new ServiceCollection();

		var result = services.AddHandlersFromAssemblyContaining<FakeCommandHandler>();

		result.Should().BeSameAs(services);
	}
}
