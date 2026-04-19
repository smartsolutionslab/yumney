using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingReadDbContext(DbContextOptions<ShoppingReadDbContext> options) : DbContext(options)
{
	public DbSet<ShoppingListReadItem> ShoppingListReadItems => Set<ShoppingListReadItem>();

	public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingDbContext).Assembly);
	}
}
