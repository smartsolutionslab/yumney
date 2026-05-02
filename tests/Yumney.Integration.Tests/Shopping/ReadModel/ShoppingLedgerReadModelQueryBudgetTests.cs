using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.ReadModel;

[Collection(AspireCollection.Name)]
public class ShoppingLedgerReadModelQueryBudgetTests(AspireFixture fixture)
{
	[Fact]
	public async Task GetByOwnerAsync_AnyRowCount_IssuesExactlyOneCommand()
	{
		var owner = OwnerIdentifier.From($"budget-{Guid.NewGuid():N}");
		await SeedAsync(owner, count: 3);

		var connectionString = await fixture.App.GetConnectionStringAsync("shoppingdb");
		await using var provider = BuildProvider(connectionString!);
		await using var scope = provider.CreateAsyncScope();

		var counter = scope.ServiceProvider.GetRequiredService<IQueryCounter>();
		var repository = ActivatorUtilities.CreateInstance<ShoppingLedgerReadModelRepository>(scope.ServiceProvider);

		counter.Reset();
		var result = await repository.GetByOwnerAsync(owner);

		counter.Count.Should().Be(1, "GetByOwnerAsync must issue a single SELECT — N+1 regression otherwise");
		result.Items.Should().HaveCount(3);
	}

	private static ServiceProvider BuildProvider(string connectionString)
	{
		ServiceCollection services = [];
		services.AddQueryCounting();
		services.AddDbContext<ShoppingReadDbContext>((sp, options) =>
		{
			options
				.UseNpgsql(connectionString)
				.AddInterceptors(sp.GetRequiredService<QueryCountingInterceptor>());
		});

		return services.BuildServiceProvider();
	}

	private async Task SeedAsync(OwnerIdentifier owner, int count)
	{
		await using var context = await fixture.CreateShoppingReadDbContextAsync();
		for (var index = 0; index < count; index++)
		{
			context.ShoppingLedgerReadItems.Add(new ShoppingLedgerReadItem
			{
				Id = Guid.CreateVersion7(),
				OwnerId = owner.Value,
				ItemName = $"Item-{index}",
				TotalQuantity = 1,
				Unit = "pc",
				LastUpdated = DateTime.UtcNow,
			});
		}

		await context.SaveChangesAsync();
	}
}
