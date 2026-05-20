using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

/// <summary>
/// Integration test fixture that boots the Aspire AppHost with E2ETests mode.
/// Starts infra (PostgreSQL, Keycloak, Redis, RabbitMQ) and all 4 API projects.
/// Skips frontend, gateway, mailpit, scalar, and LLM for fast startup.
/// Exposes HttpClients for each API and DbContext factories for seeding.
/// </summary>
#pragma warning disable SA1601
public sealed partial class AspireFixture : IAsyncLifetime
#pragma warning restore SA1601
{
	private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(8);

	private static readonly string[] DefaultUserRealmRoles = ["user"];

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

	/// <summary>Gets the pre-configured HttpClient targeting the YARP gateway.</summary>
	public HttpClient Gateway { get; private set; } = null!;

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
		Gateway = app.CreateHttpClient("yumney-gateway");

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
		Gateway.Dispose();

		if (app is not null)
		{
			await app.StopAsync();
			await ((IAsyncDisposable)app).DisposeAsync();
		}
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
