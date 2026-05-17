using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class QueryCountingServiceCollectionExtensionsTests
{
	[Fact]
	public void AddQueryCounting_RegistersIQueryCounterAsScoped()
	{
		var services = new ServiceCollection();

		services.AddQueryCounting();

		var descriptor = services.Single(service => service.ServiceType == typeof(IQueryCounter));
		descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
		descriptor.ImplementationType.Should().Be<QueryCounter>();
	}

	[Fact]
	public void AddQueryCounting_RegistersInterceptorAsScoped()
	{
		var services = new ServiceCollection();

		services.AddQueryCounting();

		var descriptor = services.Single(service => service.ServiceType == typeof(QueryCountingInterceptor));
		descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
	}

	[Fact]
	public void AddQueryCounting_ReturnsServiceCollectionForChaining()
	{
		var services = new ServiceCollection();

		var returned = services.AddQueryCounting();

		returned.Should().BeSameAs(services);
	}
}
