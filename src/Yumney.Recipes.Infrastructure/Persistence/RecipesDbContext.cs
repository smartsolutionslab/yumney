using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContext(DbContextOptions<RecipesDbContext> options) : DbContext(options)
{
	public DbSet<Recipe> Recipes => Set<Recipe>();

	public DbSet<RecipeFavorite> RecipeFavorites => Set<RecipeFavorite>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipesDbContext).Assembly);

		// Wolverine's envelope tables (under the wolverine_recipes schema) are
		// provisioned at API host startup by AutoBuildMessageStorageOnStartup —
		// not via EF migrations. State-based command handlers in this module
		// still use the legacy save-then-publish pattern; a follow-up will
		// convert them to publish-then-save without further schema changes.
	}
}
