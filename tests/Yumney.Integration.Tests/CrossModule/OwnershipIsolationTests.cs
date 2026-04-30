using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Multi-tenant ownership boundary. Every aggregate is scoped to a single
/// owner, but the only thing standing between a regression and a data leak
/// is the per-handler owner check (or owner-filtered query). These tests
/// drive two distinct Keycloak users through the public HTTP API and assert
/// that user B never sees or mutates user A's data, end-to-end.
///
/// Userland regressions this is designed to catch:
///   - Repository or read-model query missing the owner filter.
///   - Handler forgetting <c>if (aggregate.Owner != currentUser) AccessDenied</c>.
///   - Read model rebuild that strips the OwnerId column.
///   - JWT-forwarding handler swapping the wrong token onto the outbound call.
/// </summary>
[Collection(AspireCollection.Name)]
public class OwnershipIsolationTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string UserAName = "testuser";
	private const string UserAPassword = "Test1234";
	private const string UserBName = "admin";
	private const string UserBPassword = "Admin1234";

	private const int IsolationWeek = 46;

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task Recipes_OwnedByUserA_AreInvisibleAndImmutableToUserB()
	{
		using var userA = await fixture.CreateAuthenticatedClientAsync("recipes-api", UserAName, UserAPassword);
		using var userB = await fixture.CreateAuthenticatedClientAsync("recipes-api", UserBName, UserBPassword);

		var recipeId = await CreateRecipeAsync(userA, "User A Private Recipe");

		// User B cannot read user A's recipe — leaked details would be a tenant breach.
		var getResponse = await userB.GetAsync($"/api/v1/recipes/{recipeId}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User B cannot overwrite it either.
		var updatePayload = new
		{
			title = "Hijacked Title",
			ingredients = new object[] { new { name = "Water", amount = 1m, unit = "l" } },
			steps = new object[] { new { number = 1, description = "Drink." } },
		};
		var putResponse = await userB.PutAsJsonAsync($"/api/v1/recipes/{recipeId}", updatePayload);
		putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User B cannot delete it.
		var deleteResponse = await userB.DeleteAsync($"/api/v1/recipes/{recipeId}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User B cannot favorite it — same access-denied path through a
		// different command handler.
		var favoriteResponse = await userB.PostAsync($"/api/v1/recipes/{recipeId}/favorite", content: null);
		favoriteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User A still owns and sees the original, untouched.
		var ownerView = await userA.GetAsync($"/api/v1/recipes/{recipeId}");
		ownerView.StatusCode.Should().Be(HttpStatusCode.OK);
		var ownerBody = await ownerView.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		ownerBody.GetProperty("title").GetString().Should().Be("User A Private Recipe");
	}

	[Fact]
	public async Task ShoppingList_OwnedByUserA_IsHiddenFromUserB()
	{
		using var userARecipes = await fixture.CreateAuthenticatedClientAsync("recipes-api", UserAName, UserAPassword);
		using var userAShopping = await fixture.CreateAuthenticatedClientAsync("shopping-api", UserAName, UserAPassword);
		using var userBShopping = await fixture.CreateAuthenticatedClientAsync("shopping-api", UserBName, UserBPassword);

		var recipeId = await CreateRecipeAsync(userARecipes, "User A Shopping Recipe");
		var listId = await CreateShoppingListAsync(userAShopping, recipeId);

		// User B cannot read user A's list by id.
		var getById = await userBShopping.GetAsync($"/api/v1/shopping-lists/{listId}");
		getById.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User B cannot mutate user A's list either — check-all is the only
		// cross-user write attack vector that takes the list id from the URL.
		// The endpoint requires a CheckOffItemRequest body; without it ASP.NET
		// fails at model binding with 500 before the handler's owner check
		// even runs, masking the real assertion.
		var checkAllResponse = await userBShopping.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/check-all",
			new { isChecked = true });
		checkAllResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// User B's projection-backed list endpoint must not surface user A's list.
		await Eventually.AssertAsync(
			async () =>
			{
				var listResponse = await userBShopping.GetAsync("/api/v1/shopping-lists/");
				listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
				var page = await listResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var ids = page.GetProperty("items").EnumerateArray()
					.Select(i => i.GetProperty("identifier").GetGuid())
					.ToList();
				ids.Should().NotContain(listId);
			},
			timeout: TimeSpan.FromSeconds(10));

		// Owner still sees their list (sanity — a too-aggressive filter would also hide it from user A).
		await Eventually.AssertAsync(
			async () =>
			{
				var ownerView = await userAShopping.GetAsync($"/api/v1/shopping-lists/{listId}");
				ownerView.StatusCode.Should().Be(HttpStatusCode.OK);
			},
			timeout: TimeSpan.FromSeconds(10));
	}

	[Fact]
	public async Task MealPlan_AssignedByUserA_DoesNotLeakIntoUserBsWeek()
	{
		using var userAMealPlan = await fixture.CreateAuthenticatedClientAsync("mealplan-api", UserAName, UserAPassword);
		using var userBMealPlan = await fixture.CreateAuthenticatedClientAsync("mealplan-api", UserBName, UserBPassword);

		var weekPath = $"/api/v1/meal-plans/{Year}/w/{IsolationWeek}";
		var assignRequest = new
		{
			day = DayOfWeek.Friday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "User A Friday Dinner",
			mealType = 0,
			servings = 4,
		};

		var assign = await userAMealPlan.PostAsJsonAsync($"{weekPath}/slots", assignRequest);
		assign.StatusCode.Should().Be(HttpStatusCode.OK);

		// User B reads the same calendar week — every visible slot must be empty.
		// The read-model projection runs through Wolverine; poll so we don't false-pass
		// before user A's projection even committed.
		await Eventually.AssertAsync(
			async () =>
			{
				var ownerResponse = await userAMealPlan.GetAsync(weekPath);
				ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
				var ownerPlan = await ownerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var ownerHasFridayDinner = ownerPlan.GetProperty("slots").EnumerateArray().Any(s =>
					s.GetProperty("day").GetString() == "Friday" &&
					s.GetProperty("mealType").GetString() == "Dinner" &&
					!s.GetProperty("isEmpty").GetBoolean());
				ownerHasFridayDinner.Should().BeTrue("user A's projection should have caught up");
			},
			timeout: TimeSpan.FromSeconds(15));

		var otherResponse = await userBMealPlan.GetAsync(weekPath);
		otherResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var otherPlan = await otherResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var otherSlots = otherPlan.GetProperty("slots").EnumerateArray().ToList();
		otherSlots.Should().OnlyContain(s => s.GetProperty("isEmpty").GetBoolean());
	}

	private static async Task<Guid> CreateRecipeAsync(HttpClient client, string title)
	{
		var response = await client.PostAsJsonAsync("/api/v1/recipes", new
		{
			title,
			ingredients = new object[] { new { name = "Water", amount = 1m, unit = "l" } },
			steps = new object[] { new { number = 1, description = "Boil." } },
		});
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task<Guid> CreateShoppingListAsync(HttpClient client, Guid recipeId)
	{
		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/", new
		{
			title = $"Isolation-List-{Guid.NewGuid():N}",
			items = new[] { new { name = "Water", amount = 1m, unit = "l" } },
			recipeIdentifier = recipeId,
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private async Task CleanupAsync()
	{
		var userAId = await fixture.GetUserIdAsync(UserAName, UserAPassword);
		var userBId = await fixture.GetUserIdAsync(UserBName, UserBPassword);

		await CleanupShoppingForOwnerAsync(userAId);
		await CleanupShoppingForOwnerAsync(userBId);
		await CleanupRecipesForOwnerAsync(userAId);
		await CleanupRecipesForOwnerAsync(userBId);
	}

	private async Task CleanupShoppingForOwnerAsync(string userId)
	{
		var owner = OwnerIdentifier.From(userId);
		await fixture.ResetShoppingListEventStoreAsync(owner);
		await fixture.ResetShoppingEventStoreAsync(owner);
		await fixture.ResetShoppingReadModelAsync(userId);

		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>()
			.Where(s => s.OwnerId == userId).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>()
			.Where(i => i.OwnerId == userId).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		await ctx.SaveChangesAsync();
	}

	private Task CleanupRecipesForOwnerAsync(string userId) =>
		AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r =>
				r.Owner == global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier.From(userId)));
}
