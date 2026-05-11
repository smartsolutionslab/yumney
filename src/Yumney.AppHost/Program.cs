using System.Globalization;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using SmartSolutionsLab.Yumney.AppHost;
using SmartSolutionsLab.Yumney.AppHost.Options;

var builder = DistributedApplication.CreateBuilder(args);
var isRunMode = builder.ExecutionContext.IsRunMode;
var options = AppHostOptions.FromConfiguration(builder.Configuration);

if (builder.ExecutionContext.IsPublishMode)
{
	builder.AddAzureContainerAppEnvironment("cae");
}

var postgresUser = builder.AddParameter("PostgresUser");
var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true);
var messagingPassword = builder.AddParameter("MessagingPassword", secret: true);
var redisPassword = builder.AddParameter("RedisPassword", secret: true);

// Run mode: default matches the literal in Realms/yumney-realm.json so the dev
// stack works out of the box. Publish mode: no default — value must be supplied
// via Container App secret backed by Key Vault.
var yumneyApiClientSecret = isRunMode
	? builder.AddParameter("YumneyApiClientSecret", "dev-only-keycloak-client-secret", secret: true)
	: builder.AddParameter("YumneyApiClientSecret", secret: true);

IResourceBuilder<IResourceWithConnectionString> recipesDb, shoppingDb, usersDb, mealplanDb, keycloakDb;

if (options.DatabaseOnly)
{
	var postgres = builder.AddPostgres("postgres");
	recipesDb = postgres.AddDatabase("recipesdb");
	shoppingDb = postgres.AddDatabase("shoppingdb");
	usersDb = postgres.AddDatabase("usersdb");
	mealplanDb = postgres.AddDatabase("mealplandb");
	keycloakDb = postgres.AddDatabase("keycloakdb");
}
else
{
	var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
		.WithPasswordAuthentication(userName: postgresUser, password: postgresPassword)
		.RunAsContainer(pg =>
		{
			pg.WithImageTag("17-alpine");
			if (!options.E2ETests)
			{
				pg.WithDataVolume();
				pg.WithLifetime(ContainerLifetime.Persistent);
				pg.WithPgAdmin();
			}
		});
	recipesDb = postgres.AddDatabase("recipesdb");
	shoppingDb = postgres.AddDatabase("shoppingdb");
	usersDb = postgres.AddDatabase("usersdb");
	mealplanDb = postgres.AddDatabase("mealplandb");
	keycloakDb = postgres.AddDatabase("keycloakdb");
}

if (!options.DatabaseOnly)
{
	var migrationRunner = builder
		.AddProject<Projects.Yumney_MigrationRunner>("yumney-migrations")
		.WithReference(recipesDb)
		.WithReference(shoppingDb)
		.WithReference(usersDb)
		.WithReference(mealplanDb)
		.WaitFor(recipesDb)
		.WaitFor(shoppingDb)
		.WaitFor(usersDb)
		.WaitFor(mealplanDb);

	// Dashboard-only operational entries (run mode): one resource per maintenance
	// task. Each sits idle until the dev clicks Start in the dashboard.
	if (isRunMode)
	{
		DashboardResetEntries.AddShoppingAndMealPlanResetEntries(builder, recipesDb, shoppingDb, usersDb, mealplanDb);
	}

	// ── Infrastructure ── (persistent with data volumes for dev, ephemeral for E2E)
	var redis = builder
		.AddRedis("redis", password: redisPassword)
		.WithImageTag("alpine");
	var messaging = builder
		.AddRabbitMQ("messaging", password: messagingPassword)
		.WithImageTag("4-management-alpine")
		.WithManagementPlugin();
	var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword);

	if (isRunMode && !options.E2ETests)
	{
		redis.WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
		messaging.WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
		keycloak.WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
	}

	keycloak.WithRealmImport("Realms");

	if (isRunMode && !options.E2ETests)
	{
		builder
			.AddContainer("mailpit", "axllent/mailpit", "v1.22")
			.WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")
			.WithEndpoint(port: 1025, targetPort: 1025, name: "smtp")
			.WithLifetime(ContainerLifetime.Persistent);
	}

	if (!isRunMode)
	{
		var keycloakDbHost = builder.AddParameter("KeycloakDbHost");
		keycloak
			.WithEnvironment("KC_DB", "postgres")
			.WithEnvironment("KC_DB_URL_HOST", keycloakDbHost)
			.WithEnvironment("KC_DB_URL_DATABASE", "keycloakdb")
			.WithEnvironment("KC_DB_USERNAME", postgresUser)
			.WithEnvironment("KC_DB_PASSWORD", postgresPassword)
			.WithEnvironment("KC_HTTP_ENABLED", "true")
			.WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
			.WithEnvironment("KC_HOSTNAME_STRICT", "false")
			.WithEndpoint("http", e => e.IsExternal = true);
	}

	var apiEnvironment = builder.Environment.EnvironmentName;

	var usersApi = builder
		.AddProject<Projects.Yumney_Users_Api>("users-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.WithEnvironment("Keycloak__ClientSecret", yumneyApiClientSecret)
		.AsYumneyApi(options, usersDb, keycloak, redis, messaging, migrationRunner);
	var shoppingApi = builder
		.AddProject<Projects.Yumney_Shopping_Api>("shopping-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(options, shoppingDb, keycloak, redis, messaging, migrationRunner)
		.WithReference(usersApi);
	var recipesApi = builder
		.AddProject<Projects.Yumney_Recipes_Api>("recipes-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(options, recipesDb, keycloak, redis, messaging, migrationRunner)
		.WithLlmProvider(builder, options)
		.WithReference(shoppingApi)
		.WithReference(usersApi);
	var mealplanApi = builder
		.AddProject<Projects.Yumney_MealPlan_Api>("mealplan-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(options, mealplanDb, keycloak, redis, messaging, migrationRunner)
		.WithLlmProvider(builder, options)
		.WithReference(recipesApi)
		.WithReference(shoppingApi)
		.WithReference(usersApi);

	// Reverse reference: chat tools in Recipes call mealplan-api for get_weekly_plan
	// (and future planner tools). Cannot inline above because mealplanApi is
	// declared after recipesApi.
	recipesApi.WithReference(mealplanApi);

	// Reverse reference: shopping-api's CreateShoppingListFromRecipes handler
	// calls recipes-api over HTTP via HttpRecipeIngredientLookup. Without this
	// reference Aspire doesn't register service discovery for "recipes-api" in
	// the shopping host, so cross-module HTTP calls fall through to DNS for
	// the literal hostname "recipes-api" and hit the 30s Polly timeout. Same
	// recipesApi-declared-after-shoppingApi inline-impossibility as above.
	shoppingApi.WithReference(recipesApi);

	// Images are pushed to the ACR provisioned by AddAzureContainerAppEnvironment("cae");
	// ACA pulls them via the environment's managed identity, so no registry credentials
	// are baked into the container app revisions.
	void ConfigureContainerApp(AzureResourceInfrastructure infra, ContainerApp app, int minReplicas, int maxReplicas, int? concurrentRequests = null)
	{
		app.Template.Scale.MinReplicas = minReplicas;
		app.Template.Scale.MaxReplicas = maxReplicas;

		if (concurrentRequests.HasValue)
		{
			app.Template.Scale.Rules.Add(new ContainerAppScaleRule
			{
				Name = "http-scaling",
				Http = new ContainerAppHttpScaleRule
				{
					Metadata =
				{
					{
						"concurrentRequests",
						concurrentRequests.Value.ToString(CultureInfo.InvariantCulture)
					},
				},
				},
			});
		}
	}

	recipesApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 1, 10, 20));
	shoppingApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 5, 50));
	usersApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 3, 50));
	mealplanApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 3, 50));
	migrationRunner.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 1));

	// MCP server: aggregates the per-host capability manifests on startup and
	// exposes them as MCP tools at /mcp (Phase 4b). Tool invocation returns a
	// stub until the OAuth bridge + REST proxy land in Phase 4c. Declared
	// outside the run/publish branches because both gateways route to it.
	var mcpServer = builder.AddProject<Projects.Yumney_Mcp_Server>("mcp-server")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.WithReference(keycloak)
		.WithReference(recipesApi)
		.WithReference(shoppingApi)
		.WithReference(mealplanApi);

	if (isRunMode)
	{
		// Run mode (including E2E) spawns the Angular dev servers and the gateway
		// project so the full federated stack is reachable on localhost. The E2E
		// flag above still toggles persistent volumes and optional sidecars
		// (pgAdmin, mailpit); it doesn't affect whether the frontend is registered.
		var addMfe = (string name, string script, int port) =>
#pragma warning disable ASPIREBROWSERLOGS001
			builder.AddJavaScriptApp(name, "../../client", script)
				.WithYarn()
				.WithEnvironment("NX_DAEMON", "false")
				.WithEnvironment("NX_ISOLATE_PLUGINS", "false")
				.WithHttpEndpoint(targetPort: port)
				.WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

		// E2E mode runs against a production build so PWA tests (service
		// worker, offline cache) and Vite-free fetch behaviour exercise the
		// real shape. `yarn build:all` co-locates every MFE in
		// `dist/apps/shell/browser`, then `serve:shell:dist` hands it out
		// from a single static root.
		var shell = options.E2ETests
			? addMfe("shell", "serve:shell:dist", 4200)
			: addMfe("shell", "serve:shell", 4200);

		if (!options.E2ETests)
		{
			addMfe("recipes-mfe", "serve:recipes", 4201);
			addMfe("shopping-mfe", "serve:shopping", 4202);
			addMfe("account-mfe", "serve:account", 4203);
		}

		builder.AddProject<Projects.Yumney_Gateway>("yumney-gateway")
			.WithHttpEndpoint(port: 5100)
			.WithReference(recipesApi)
			.WithReference(shoppingApi)
			.WithReference(usersApi)
			.WithReference(mealplanApi)
			.WithReference(mcpServer)
			.WithReference(shell)
			.WithReference(keycloak)
			.WaitFor(keycloak);
	}
	else if (!isRunMode)
	{
		var frontend = builder
			.AddDockerfile("yumney-frontend", "../../client", "docker/Dockerfile")
			.WithHttpEndpoint(targetPort: 80);

		builder
			.AddYarp("yumney-gateway")
			.WithConfiguration(yarp =>
			{
				yarp.AddRoute("/api/v1/recipes/{**catch-all}", recipesApi);
				yarp.AddRoute("/api/v1/shopping-lists/{**catch-all}", shoppingApi);
				yarp.AddRoute("/api/v1/auth/{**catch-all}", usersApi);
				yarp.AddRoute("/api/v1/meal-plans/{**catch-all}", mealplanApi);
				yarp.AddRoute("/mcp/{**catch-all}", mcpServer);
				yarp.AddRoute("/{**catch-all}", frontend.GetEndpoint("http"));
			})
			.WithExternalHttpEndpoints();

		frontend.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 1, 5, 100));
	}
}

builder.Build().Run();
