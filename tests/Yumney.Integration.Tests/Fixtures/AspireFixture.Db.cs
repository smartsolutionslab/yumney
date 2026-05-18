using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using ShoppingDomain = SmartSolutionsLab.Yumney.Shopping.Domain;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

#pragma warning disable SA1601
public sealed partial class AspireFixture
#pragma warning restore SA1601
{
	public async Task<RecipesDbContext> CreateRecipesDbContextAsync()
	{
		var connectionString = await App.GetConnectionStringAsync("recipesdb");
		var optionsBuilder = new DbContextOptionsBuilder<RecipesDbContext>();
		optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
		return new RecipesDbContext(optionsBuilder.Options);
	}

	public async Task SeedRecipesAsync(params Recipe[] recipes)
	{
		await using var context = await CreateRecipesDbContextAsync();
		context.Recipes.AddRange(recipes);
		await context.SaveChangesAsync();
	}

	public async Task<ShoppingDbContext> CreateShoppingDbContextAsync()
	{
		var connectionString = await App.GetConnectionStringAsync("shoppingdb");
		var optionsBuilder = new DbContextOptionsBuilder<ShoppingDbContext>();
		optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
		return new ShoppingDbContext(optionsBuilder.Options);
	}

	public async Task<ShoppingReadDbContext> CreateShoppingReadDbContextAsync()
	{
		var connectionString = await App.GetConnectionStringAsync("shoppingdb");
		var optionsBuilder = new DbContextOptionsBuilder<ShoppingReadDbContext>();
		optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
		return new ShoppingReadDbContext(optionsBuilder.Options);
	}

	public async Task ResetShoppingEventStoreAsync(ShoppingDomain.ShoppingList.OwnerIdentifier owner)
	{
		await using var context = await CreateShoppingDbContextAsync();
		var aggregateIds = await context.Set<AggregateMetadata>()
			.Where(metadata => metadata.OwnerId == owner.Value)
			.Select(metadata => metadata.AggregateId)
			.ToListAsync();

		if (aggregateIds.Count == 0) return;

		var events = await context.Set<StoredEvent>()
			.Where(stored => aggregateIds.Contains(stored.AggregateId))
			.ToListAsync();
		var metadata = await context.Set<AggregateMetadata>()
			.Where(row => aggregateIds.Contains(row.AggregateId))
			.ToListAsync();

		context.RemoveRange(events);
		context.RemoveRange(metadata);
		await context.SaveChangesAsync();
	}

	public async Task ResetShoppingListEventStoreAsync(ShoppingDomain.ShoppingList.OwnerIdentifier owner)
	{
		await using var writeContext = await CreateShoppingDbContextAsync();
		var aggregateIds = await writeContext.Set<ShoppingListAggregateMetadata>()
			.Where(metadata => metadata.OwnerId == owner.Value)
			.Select(metadata => metadata.AggregateId)
			.ToListAsync();

		if (aggregateIds.Count > 0)
		{
			await writeContext.Set<ShoppingListStoredEvent>()
				.Where(stored => aggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync();
			await writeContext.Set<ShoppingListAggregateMetadata>()
				.Where(row => aggregateIds.Contains(row.AggregateId))
				.ExecuteDeleteAsync();
		}

		// Read model lives in a separate set of tables — the event-store wipe
		// above doesn't touch it. Without this, lists materialised by the
		// projection handler in earlier tests stay visible to subsequent
		// reads, surfacing as "expected 0 lists, found N" assertion failures
		// in the integration test suite (the umbrella backend job that runs
		// every Shopping integration class against one shared database).
		await using var readContext = await CreateShoppingReadDbContextAsync();
		await readContext.Set<ShoppingListItemReadItem>()
			.Where(item => item.OwnerId == owner.Value)
			.ExecuteDeleteAsync();
		await readContext.Set<ShoppingListSummaryReadItem>()
			.Where(summary => summary.OwnerId == owner.Value)
			.ExecuteDeleteAsync();
	}

	public async Task ResetShoppingReadModelAsync(string ownerId)
	{
		await using var context = await CreateShoppingDbContextAsync();
		var items = await context.Set<ShoppingLedgerReadItem>()
			.Where(row => row.OwnerId == ownerId)
			.ToListAsync();

		if (items.Count == 0) return;

		context.RemoveRange(items);
		await context.SaveChangesAsync();
	}

	public async Task<UsersDbContext> CreateUsersDbContextAsync()
	{
		var connectionString = await App.GetConnectionStringAsync("usersdb");
		var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
		optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());

		return new UsersDbContext(optionsBuilder.Options);
	}

	public async Task SeedUserProfilesAsync(params AppUserProfile[] profiles)
	{
		await using var context = await CreateUsersDbContextAsync();
		context.AppUserProfiles.AddRange(profiles);

		await context.SaveChangesAsync();
	}
}
