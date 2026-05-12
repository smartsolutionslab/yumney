using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// End-to-end GDPR Art. 17 cascade: DELETE /api/v1/users/me publishes
/// <c>UserAccountDeletedIntegrationEvent</c>, every other module subscribes
/// and purges the owner's data. The Users-module portion (profile, activity,
/// staples) is wiped in-process before the Keycloak delete.
/// Uses a freshly-registered user so the shared <c>testuser</c> stays usable
/// for the rest of the integration suite.
/// </summary>
[Collection(AspireCollection.Name)]
public class AccountDeletionCascadeTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task DeleteAccount_WithSeededData_PurgesEveryModule()
	{
		var (email, password) = ($"cascade-{Guid.NewGuid():N}@yumney.dev", "Valid1Pass");
		await RegisterUserAsync(email, password);

		var token = await fixture.GetAccessTokenAsync(email, password);
		var userId = DecodeSubClaim(token);

		using var users = AuthenticatedClient(fixture.UsersApi, token);
		using var recipes = AuthenticatedClient(fixture.RecipesApi, token);
		using var shopping = AuthenticatedClient(fixture.ShoppingApi, token);
		using var mealplan = AuthenticatedClient(fixture.MealPlanApi, token);

		// JIT-provision the profile, then seed one row per module.
		(await users.GetAsync("/api/v1/users/me/profile")).EnsureSuccessStatusCode();
		var recipeId = await CreateRecipeAsync(recipes);
		await CreateShoppingListAsync(shopping, recipeId);
		await AssignMealSlotAsync(mealplan, recipeId);

		await WaitForAsync(async () =>
			await CountAcrossAllModulesAsync(userId) > 0,
			because: "seeding must land before the delete fires");

		var delete = await users.DeleteAsync("/api/v1/users/me");
		delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

		await WaitForAsync(async () =>
			await CountAcrossAllModulesAsync(userId) == 0,
			because: "every subscribing module must purge the owner's rows after UserAccountDeletedIntegrationEvent");
	}

	[Fact]
	public async Task DeleteAccount_WithNoModuleData_StillReturns204()
	{
		var (email, password) = ($"empty-{Guid.NewGuid():N}@yumney.dev", "Valid1Pass");
		await RegisterUserAsync(email, password);

		var token = await fixture.GetAccessTokenAsync(email, password);
		using var users = AuthenticatedClient(fixture.UsersApi, token);

		var delete = await users.DeleteAsync("/api/v1/users/me");

		delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	private async Task<int> CountAcrossAllModulesAsync(string userId)
	{
		await using var usersCtx = await fixture.CreateUsersDbContextAsync();
		await using var recipesCtx = await fixture.CreateRecipesDbContextAsync();
		await using var shoppingCtx = await fixture.CreateShoppingDbContextAsync();
		await using var shoppingReadCtx = await fixture.CreateShoppingReadDbContextAsync();
		await using var mealPlanCtx = await CreateMealPlanDbContextAsync();

		var profileCount = await usersCtx.AppUserProfiles.CountAsync(profile => profile.KeycloakUserId.Value == userId);
		var recipeCount = await recipesCtx.Recipes.CountAsync(recipe => recipe.Owner.Value == userId);
		var shoppingAggregateCount = await shoppingCtx.ShoppingListAggregates.CountAsync(aggregate => aggregate.OwnerId == userId);
		var shoppingReadCount = await shoppingReadCtx.Set<ShoppingListSummaryReadItem>().CountAsync(row => row.OwnerId == userId);
		var mealPlanAggregateCount = await mealPlanCtx.MealPlanAggregates.CountAsync(aggregate => aggregate.OwnerId == userId);

		return profileCount + recipeCount + shoppingAggregateCount + shoppingReadCount + mealPlanAggregateCount;
	}

	private async Task<MealPlanDbContext> CreateMealPlanDbContextAsync()
	{
		var connectionString = await fixture.App.GetConnectionStringAsync("mealplandb");
		var optionsBuilder = new DbContextOptionsBuilder<MealPlanDbContext>();
		optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
		return new MealPlanDbContext(optionsBuilder.Options);
	}

	private async Task RegisterUserAsync(string email, string password)
	{
		var response = await fixture.UsersApi.PostAsJsonAsync("/api/v1/auth/register", new
		{
			email,
			password,
			displayName = $"Cascade {email}",
		});
		response.EnsureSuccessStatusCode();
	}

	private static HttpClient AuthenticatedClient(HttpClient source, string token)
	{
		var copy = new HttpClient { BaseAddress = source.BaseAddress };
		copy.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return copy;
	}

	private static string DecodeSubClaim(string token)
	{
		var payload = token.Split('.')[1];
		var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
		var decoded = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
		var claims = JsonSerializer.Deserialize<JsonElement>(decoded);
		return claims.GetProperty("sub").GetString()!;
	}

	private static async Task<Guid> CreateRecipeAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/recipes", new
		{
			title = $"Cascade-Recipe-{Guid.NewGuid():N}",
			ingredients = new object[] { new { name = "Water", amount = 1m, unit = "l" } },
			steps = new object[] { new { number = 1, description = "Boil." } },
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task CreateShoppingListAsync(HttpClient client, Guid recipeId)
	{
		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/", new
		{
			title = $"Cascade-List-{Guid.NewGuid():N}",
			items = new[] { new { name = "Water", amount = 1m, unit = "l" } },
			recipeIdentifier = recipeId,
		});
		response.EnsureSuccessStatusCode();
	}

	private static async Task AssignMealSlotAsync(HttpClient client, Guid recipeId)
	{
		var now = DateTime.UtcNow;
		var year = System.Globalization.ISOWeek.GetYear(now);
		var week = System.Globalization.ISOWeek.GetWeekOfYear(now);

		var response = await client.PostAsJsonAsync(
			$"/api/v1/meal-plans/{year}/w/{week}/slots",
			new
			{
				day = "Monday",
				recipeIdentifier = recipeId,
				recipeTitle = "Cascade Recipe",
				mealType = "Dinner",
			});
		response.EnsureSuccessStatusCode();
	}

	private static async Task WaitForAsync(Func<Task<bool>> predicate, string because, int timeoutSeconds = 15)
	{
		var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
		while (DateTime.UtcNow < deadline)
		{
			if (await predicate()) return;
			await Task.Delay(250);
		}

		throw new TimeoutException($"Condition not met within {timeoutSeconds}s — {because}");
	}
}
