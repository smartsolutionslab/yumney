using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Hosting;

public class ModuleRegistrationExtensionsTests
{
	[Fact]
	public void RegisterServices_WithMultipleModules_CallsEachOnceInOrder()
	{
		var builder = WebApplication.CreateBuilder();
		var moduleA = Substitute.For<IModule>();
		var moduleB = Substitute.For<IModule>();
		moduleA.RegisterServices(builder).Returns(builder);
		moduleB.RegisterServices(builder).Returns(builder);

		var result = builder.RegisterServices(moduleA, moduleB);

		result.Should().BeSameAs(builder);
		Received.InOrder(() =>
		{
			moduleA.RegisterServices(builder);
			moduleB.RegisterServices(builder);
		});
	}

	[Fact]
	public void RegisterServices_NoModules_ReturnsBuilderUnchanged()
	{
		var builder = WebApplication.CreateBuilder();

		var result = builder.RegisterServices();

		result.Should().BeSameAs(builder);
	}

	[Fact]
	public void RegisterEndpoints_FiltersToIEndpointModuleOnly()
	{
		var builder = WebApplication.CreateBuilder();
		var app = builder.Build();
		var plainModule = Substitute.For<IModule>();
		var endpointModule = Substitute.For<IEndpointModule>();
		endpointModule.RegisterEndpoints(app).Returns(app);

		var result = app.RegisterEndpoints(plainModule, endpointModule);

		result.Should().BeSameAs(app);
		endpointModule.Received(1).RegisterEndpoints(app);
		plainModule.DidNotReceiveWithAnyArgs().RegisterServices(default!);
	}

	[Fact]
	public void RegisterEndpoints_OnlyEndpointModules_CallsEachOnce()
	{
		var builder = WebApplication.CreateBuilder();
		var app = builder.Build();
		var moduleA = Substitute.For<IEndpointModule>();
		var moduleB = Substitute.For<IEndpointModule>();
		moduleA.RegisterEndpoints(app).Returns(app);
		moduleB.RegisterEndpoints(app).Returns(app);

		app.RegisterEndpoints(moduleA, moduleB);

		moduleA.Received(1).RegisterEndpoints(app);
		moduleB.Received(1).RegisterEndpoints(app);
	}

	[Fact]
	public void RegisterEndpoints_NoModules_ReturnsAppUnchanged()
	{
		var builder = WebApplication.CreateBuilder();
		var app = builder.Build();

		var result = app.RegisterEndpoints();

		result.Should().BeSameAs(app);
	}
}
