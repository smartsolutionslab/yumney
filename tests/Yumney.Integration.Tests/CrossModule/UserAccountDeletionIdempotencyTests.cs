using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;
using MealPlanOwner = SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier;
using RecipeOwner = SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Each module's UserAccountDataPurger is the consumer the Wolverine
/// outbox calls when <c>UserAccountDeletedIntegrationEvent</c> fires. A
/// transient consumer error triggers Wolverine to redeliver the same event,
/// so the purger MUST be safe to call twice with the same owner — first
/// call drains the rows, second call must short-circuit (or no-op) without
/// throwing. These tests pin that invariant directly at the purger seam
/// rather than trying to fake Wolverine redelivery end-to-end.
/// </summary>
[Collection(AspireCollection.Name)]
public class UserAccountDeletionIdempotencyTests(AspireFixture fixture)
{
	[Fact]
	public async Task RecipesUserDataPurger_CalledTwiceForSameOwner_DoesNotThrowAndLeavesNoRows()
	{
		var owner = RecipeOwner.From($"idempotency-recipes-{Guid.NewGuid():N}");
		var recipe = RecipeBuilder.A().OwnedBy(owner).Build();
		await fixture.SeedRecipesAsync(recipe);

		await PurgeRecipesAsync(owner);
		var act = () => PurgeRecipesAsync(owner);

		await act.Should().NotThrowAsync();
		await using var ctx = await fixture.CreateRecipesDbContextAsync();
		var remaining = await ctx.Recipes.CountAsync(r => r.Owner == owner);
		remaining.Should().Be(0);
	}

	[Fact]
	public async Task ShoppingUserDataPurger_CalledTwiceForSameOwner_DoesNotThrow()
	{
		var owner = OwnerIdentifier.From($"idempotency-shopping-{Guid.NewGuid():N}");

		// No seed needed — purger must survive an empty state too, which is the
		// most-common redelivery case (consumer already purged on first try).
		await PurgeShoppingAsync(owner);
		var act = () => PurgeShoppingAsync(owner);

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task MealPlanUserDataPurger_CalledTwiceForSameOwner_DoesNotThrow()
	{
		var owner = MealPlanOwner.From($"idempotency-mealplan-{Guid.NewGuid():N}");

		await PurgeMealPlanAsync(owner);
		var act = () => PurgeMealPlanAsync(owner);

		await act.Should().NotThrowAsync();
	}

	private async Task PurgeRecipesAsync(RecipeOwner owner)
	{
		await using var ctx = await fixture.CreateRecipesDbContextAsync();
		var purger = new RecipesUserDataPurger(ctx);
		await purger.PurgeAsync(owner);
	}

	private async Task PurgeShoppingAsync(OwnerIdentifier owner)
	{
		await using var writeCtx = await fixture.CreateShoppingDbContextAsync();
		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var purger = new ShoppingUserDataPurger(writeCtx, readCtx);
		await purger.PurgeAsync(owner);
	}

	private async Task PurgeMealPlanAsync(MealPlanOwner owner)
	{
		await using var writeCtx = await CreateMealPlanDbContextAsync();
		await using var readCtx = await CreateMealPlanReadDbContextAsync();
		var purger = new MealPlanUserDataPurger(writeCtx, readCtx);
		await purger.PurgeAsync(owner);
	}

	private async Task<MealPlanDbContext> CreateMealPlanDbContextAsync()
	{
		var connectionString = await fixture.App.GetConnectionStringAsync("mealplandb");
		var optionsBuilder = new DbContextOptionsBuilder<MealPlanDbContext>();
		optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
		return new MealPlanDbContext(optionsBuilder.Options);
	}

	private async Task<MealPlanReadDbContext> CreateMealPlanReadDbContextAsync()
	{
		var connectionString = await fixture.App.GetConnectionStringAsync("mealplandb");
		var optionsBuilder = new DbContextOptionsBuilder<MealPlanReadDbContext>();
		optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
		return new MealPlanReadDbContext(optionsBuilder.Options);
	}
}
