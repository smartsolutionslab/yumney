using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using Xunit;
using ShoppingDomain = SmartSolutionsLab.Yumney.Shopping.Domain;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

/// <summary>
/// Integration test fixture that boots the Aspire AppHost with E2ETests mode.
/// Starts infra (PostgreSQL, Keycloak, Redis, RabbitMQ) and all 4 API projects.
/// Skips frontend, gateway, mailpit, scalar, and LLM for fast startup.
/// Exposes HttpClients for each API and DbContext factories for seeding.
/// </summary>
public sealed class AspireFixture : IAsyncLifetime
{
	private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(8);

	// Hosts add JsonStringEnumConverter via ConfigureHttpJsonOptions, so enums
	// (CapabilitySurface, ChatActionType, …) are serialised as strings on the
	// wire. Default GetFromJsonAsync<T> options cannot read those strings back
	// into the enum types. Tests must deserialise responses with this options
	// instance so the round-trip stays consistent.
	public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	public static async Task CleanupAsync<TContext>(Func<Task<TContext>> contextFactory, Func<TContext, IQueryable<object>> query)
		where TContext : DbContext
	{
		await using var context = await contextFactory();
		var entities = await query(context).ToListAsync();
		context.RemoveRange(entities);
		await context.SaveChangesAsync();
	}

	private DistributedApplication? app;

	public DistributedApplication App => app ?? throw new InvalidOperationException("Aspire app not started");

	/// <summary>Gets the pre-configured HttpClient targeting the Recipes API.</summary>
	public HttpClient RecipesApi { get; private set; } = null!;

	/// <summary>Gets the pre-configured HttpClient targeting the Shopping API.</summary>
	public HttpClient ShoppingApi { get; private set; } = null!;

	/// <summary>Gets the pre-configured HttpClient targeting the Users API.</summary>
	public HttpClient UsersApi { get; private set; } = null!;

	/// <summary>Gets the pre-configured HttpClient targeting the MealPlan API.</summary>
	public HttpClient MealPlanApi { get; private set; } = null!;

	/// <summary>Gets the pre-configured HttpClient targeting the MCP server.</summary>
	public HttpClient McpServer { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		await CleanupStaleContainersAsync();

		string[] parameters =
		[
			"Parameters:PostgresUser=testuser",
			"Parameters:PostgresPassword=testpassword",
			"Parameters:KeycloakPassword=testkeycloak",
			"Parameters:MessagingPassword=testmessaging",
			"Parameters:RedisPassword=testredis",
			"E2ETests=true"
		];
		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Yumney_AppHost>(parameters);

		builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

		app = await builder.BuildAsync();

		using var cts = new CancellationTokenSource(StartupTimeout);

		try
		{
			await app.StartAsync(cts.Token);

			await app.ResourceNotifications.WaitForResourceAsync("keycloak", KnownResourceStates.Running, cts.Token);
			await app.ResourceNotifications.WaitForResourceAsync("yumney-migrations", KnownResourceStates.Finished, cts.Token);

			string[] apis = ["recipes-api", "shopping-api", "users-api", "mealplan-api", "mcp-server"];
			var apiTasks = apis.Select(api => app.ResourceNotifications.WaitForResourceAsync(api, KnownResourceStates.Running, cts.Token));

			await Task.WhenAll(apiTasks);

			// Keycloak realm import may still be in progress after container is Running.
			// Verify the token endpoint is reachable before proceeding.
			await VerifyKeycloak();
		}
		catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
		{
			throw new TimeoutException(
				$"Aspire AppHost did not start within {StartupTimeout.TotalMinutes} minutes. " +
				$"Check for port conflicts (docker ps) or stale containers. Inner: {ex.Message}",
				ex);
		}

		RecipesApi = app.CreateHttpClient("recipes-api");
		ShoppingApi = app.CreateHttpClient("shopping-api");
		UsersApi = app.CreateHttpClient("users-api");
		MealPlanApi = app.CreateHttpClient("mealplan-api");
		McpServer = app.CreateHttpClient("mcp-server");

		async Task VerifyKeycloak()
		{
			var keycloakClient = app.CreateHttpClient("keycloak");
			for (var index = 0; index < 30; index++)
			{
				try
				{
					var probe = await keycloakClient.GetAsync("/realms/yumney/.well-known/openid-configuration", cts.Token);
					if (probe.IsSuccessStatusCode) break;
				}
				catch
				{
					// Not ready yet
				}

				await Task.Delay(1000, cts.Token);
			}
		}
	}

	public async Task DisposeAsync()
	{
		RecipesApi.Dispose();
		ShoppingApi.Dispose();
		UsersApi.Dispose();
		MealPlanApi.Dispose();
		McpServer.Dispose();

		if (app is not null)
		{
			await app.StopAsync();
			await ((IAsyncDisposable)app).DisposeAsync();
		}
	}

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

	public Task<HttpClient> CreateAuthenticatedClientAsync(string resourceName) =>
		CreateAuthenticatedClientAsync(resourceName, "testuser", "Test1234");

	public async Task<HttpClient> CreateAuthenticatedClientAsync(string resourceName, string username, string password)
	{
		var accessToken = await GetAccessTokenAsync(username, password);
		var client = App.CreateHttpClient(resourceName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		return client;
	}

	public Task<string> GetTestUserIdAsync() => GetUserIdAsync("testuser", "Test1234");

	public async Task<string> GetUserIdAsync(string username, string password)
	{
		var accessToken = await GetAccessTokenAsync(username, password);
		var payload = accessToken.Split('.')[1];
		var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
		var decoded = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
		var claims = JsonSerializer.Deserialize<JsonElement>(decoded);

		return claims.GetProperty("sub").GetString()!;
	}

	public async Task<string> GetAccessTokenAsync(string username, string password)
	{
		var keycloakClient = App.CreateHttpClient("keycloak");
		Dictionary<string, string> valueCollection = new()
		{
			["grant_type"] = "password",
			["client_id"] = "yumney-web",
			["username"] = username,
			["password"] = password,
		};
		var tokenResponse = await keycloakClient.PostAsync("/realms/yumney/protocol/openid-connect/token", new FormUrlEncodedContent(valueCollection));

		if (!tokenResponse.IsSuccessStatusCode)
		{
			var body = await tokenResponse.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"Keycloak password grant failed for '{username}': {tokenResponse.StatusCode} {body}");
		}

		var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

		return tokenJson.GetProperty("access_token").GetString()!;
	}

	/// <summary>
	/// Provision a brand-new Keycloak user via the admin API with emailVerified=true
	/// and no requiredActions, so password-grant login works immediately.
	/// /auth/register can't be used here because the registration handler creates
	/// users with a VERIFY_EMAIL requiredAction that blocks login regardless of the
	/// realm's verifyEmail setting.
	/// </summary>
	/// <param name="emailPrefix">Prefix for the generated email address; final form is {prefix}-{guid}@yumney.dev.</param>
	/// <returns>Tuple of (KeycloakUserId from the new user's <c>sub</c> claim, generated Email, generated Password). Caller is responsible for cleanup if the test doesn't delete the account.</returns>
	public async Task<(string KeycloakUserId, string Email, string Password)> CreateKeycloakUserAsync(string emailPrefix = "test")
	{
		const string password = "Valid1Pass";
		var email = $"{emailPrefix}-{Guid.NewGuid():N}@yumney.dev";

		var keycloak = App.CreateHttpClient("keycloak");
		var adminToken = await GetMasterRealmAdminTokenAsync(keycloak);

		using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/realms/yumney/users")
		{
			Content = JsonContent.Create(new
			{
				username = email,
				email,
				enabled = true,
				emailVerified = true,
				firstName = $"Test {email}",
				credentials = new[] { new { type = "password", value = password, temporary = false } },
				realmRoles = new[] { "user" },
				requiredActions = Array.Empty<string>(),
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var response = await keycloak.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			var body = await response.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"Keycloak admin user creation failed: {response.StatusCode} {body}");
		}

		// Location header is /admin/realms/yumney/users/{guid}
		var location = response.Headers.Location?.ToString()
			?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user");
		var keycloakUserId = location.Split('/').Last();
		return (keycloakUserId, email, password);
	}

	private static async Task<string> GetMasterRealmAdminTokenAsync(HttpClient keycloak)
	{
		var form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "password",
			["client_id"] = "admin-cli",
			["username"] = "admin",
			["password"] = "testkeycloak",
		});
		var response = await keycloak.PostAsync("/realms/master/protocol/openid-connect/token", form);
		response.EnsureSuccessStatusCode();
		var tokenJson = await response.Content.ReadFromJsonAsync<JsonElement>();
		return tokenJson.GetProperty("access_token").GetString()!;
	}

#pragma warning disable SA1204
	private static async Task CleanupStaleContainersAsync()
	{
		try
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "docker",
				Arguments = "ps -aq --filter label=com.microsoft.developer.usvc-dev.build",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			});

			if (process is null) return;

			var output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			var ids = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (ids.Length == 0) return;

			var rmProcess = Process.Start(new ProcessStartInfo
			{
				FileName = "docker",
				Arguments = $"rm -f {string.Join(' ', ids)}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			});

			if (rmProcess is not null) await rmProcess.WaitForExitAsync();
		}
		catch
		{
			// Docker not available — safe to ignore
		}
	}
}
