using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingDbContext(DbContextOptions<ShoppingDbContext> options) : DbContext(options)
{
	public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

	public DbSet<StoredEvent> ShoppingEvents => Set<StoredEvent>();

	public DbSet<StoredSnapshot> ShoppingSnapshots => Set<StoredSnapshot>();

	public DbSet<AggregateMetadata> ShoppingAggregates => Set<AggregateMetadata>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingDbContext).Assembly);
	}
}
