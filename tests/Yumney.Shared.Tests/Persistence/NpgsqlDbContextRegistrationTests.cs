using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class NpgsqlDbContextRegistrationTests
{
	[Fact]
	public void AddYumneyNpgsqlDbContext_RegistersDbContextOfRequestedType()
	{
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:test"] = "Host=localhost;Database=test;Username=test;Password=test",
			})
			.Build();

		services.AddYumneyNpgsqlDbContext<FakeContext>(
			configuration,
			connectionName: "test",
			migrationsHistoryTable: "__Migrations");

		services.Should().Contain(descriptor => descriptor.ServiceType == typeof(FakeContext));
	}

	[Fact]
	public void AddYumneyNpgsqlDbContext_ReturnsServiceCollection_ForChaining()
	{
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder().Build();

		var returned = services.AddYumneyNpgsqlDbContext<FakeContext>(
			configuration,
			connectionName: "test",
			migrationsHistoryTable: "__Migrations");

		returned.Should().BeSameAs(services);
	}

	private sealed class FakeContext(DbContextOptions<FakeContext> options) : DbContext(options);
}
