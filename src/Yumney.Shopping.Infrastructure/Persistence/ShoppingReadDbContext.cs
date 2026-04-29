using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingReadDbContext(DbContextOptions<ShoppingReadDbContext> options) : DbContext(options)
{
	public DbSet<ShoppingLedgerReadItem> ShoppingLedgerReadItems => Set<ShoppingLedgerReadItem>();

	public DbSet<ShoppingListSummaryReadItem> ShoppingListSummaryReadItems => Set<ShoppingListSummaryReadItem>();

	public DbSet<ShoppingListItemReadItem> ShoppingListItemReadItems => Set<ShoppingListItemReadItem>();

	public DbSet<IngredientBalanceReadItem> IngredientBalanceReadItems => Set<IngredientBalanceReadItem>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingDbContext).Assembly);
	}
}
