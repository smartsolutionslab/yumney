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
	var migrationRunner = builder.AddProject<Projects.Yumney_MigrationRunner>("yumney-migrations")
		.WithReference(recipesDb)
		.WithReference(shoppingDb)
		.WithReference(usersDb)
		.WithReference(mealplanDb)
		.WaitFor(recipesDb)
		.WaitFor(shoppingDb)
		.WaitFor(usersDb)
		.WaitFor(mealplanDb);

	// ── Infrastructure ── (persistent with data volumes for dev, ephemeral for E2E)
	var redis = builder.AddRedis("redis", password: redisPassword).WithImageTag("alpine");
	var messaging = builder.AddRabbitMQ("messaging", password: messagingPassword).WithImageTag("4-management-alpine").WithManagementPlugin();
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
		builder.AddContainer("mailpit", "axllent/mailpit", "v1.22")
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

	var recipesApi = builder
		.AddProject<Projects.Yumney_Recipes_Api>("recipes-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(recipesDb, keycloak, redis, messaging, migrationRunner)
		.WithLlmProvider(builder, options);
	var shoppingApi = builder
		.AddProject<Projects.Yumney_Shopping_Api>("shopping-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(shoppingDb, keycloak, redis, messaging, migrationRunner);
	var usersApi = builder
		.AddProject<Projects.Yumney_Users_Api>("users-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(usersDb, keycloak, redis, messaging, migrationRunner);
	var mealplanApi = builder
		.AddProject<Projects.Yumney_MealPlan_Api>("mealplan-api")
		.WithEnvironment("ASPNETCORE_ENVIRONMENT", apiEnvironment)
		.AsYumneyApi(mealplanDb, keycloak, redis, messaging, migrationRunner)
		.WithReference(recipesApi)
		.WithReference(shoppingApi)
		.WithReference(usersApi);

	// ── Container Registry (GHCR for CI/CD) ──
#pragma warning disable ASPIRECOMPUTE003
	var registry = options.UseGhcr
		? builder.AddContainerRegistry("ghcr", options.RegistryEndpoint!, options.RegistryRepository!)
		: null;

	if (registry is not null)
	{
		migrationRunner.WithContainerRegistry(registry);
		recipesApi.WithContainerRegistry(registry);
		shoppingApi.WithContainerRegistry(registry);
		usersApi.WithContainerRegistry(registry);
		mealplanApi.WithContainerRegistry(registry);
	}
#pragma warning restore ASPIRECOMPUTE003

	// ── ACA Scaling + GHCR pull credentials ──
	void ConfigureContainerApp(AzureResourceInfrastructure infra, ContainerApp app, int minReplicas, int maxReplicas, int? concurrentRequests = null)
	{
		if (options.UseGhcrPullCredentials)
		{
			app.Configuration.Registries.Add(new ContainerAppRegistryCredentials
			{
				Server = "ghcr.io",
				Username = options.GhcrUser!,
				PasswordSecretRef = "ghcr-token",
			});
			app.Configuration.Secrets.Add(new ContainerAppWritableSecret
			{
				Name = "ghcr-token",
				Value = options.GhcrToken!,
			});
		}

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

	if (isRunMode)
	{
		// Run mode (including E2E) spawns the Angular dev servers and the gateway
		// project so the full federated stack is reachable on localhost. The E2E
		// flag above still toggles persistent volumes and optional sidecars
		// (pgAdmin, mailpit); it doesn't affect whether the frontend is registered.
		var addMfe = (string name, string script, int port) =>
			builder.AddJavaScriptApp(name, "../../client", script)
				.WithYarn()
				.WithEnvironment("NX_DAEMON", "false")
				.WithEnvironment("NX_ISOLATE_PLUGINS", "false")
				.WithHttpEndpoint(targetPort: port);

		var shell = addMfe("shell", "serve:shell", 4200);
		var recipesMfe = addMfe("recipes-mfe", "serve:recipes", 4201);
		var shoppingMfe = addMfe("shopping-mfe", "serve:shopping", 4202);
		var accountMfe = addMfe("account-mfe", "serve:account", 4203);

		builder.AddProject<Projects.Yumney_Gateway>("yumney-gateway")
			.WithHttpEndpoint(port: 5100)
			.WithReference(recipesApi)
			.WithReference(shoppingApi)
			.WithReference(usersApi)
			.WithReference(mealplanApi)
			.WithReference(shell)
			.WithReference(keycloak)
			.WaitFor(keycloak);
	}
	else if (!isRunMode)
	{
		var frontend = builder
			.AddDockerfile("yumney-frontend", "../../client", "docker/Dockerfile")
			.WithHttpEndpoint(targetPort: 80);

#pragma warning disable ASPIRECOMPUTE003
		if (registry is not null)
		{
			frontend.WithContainerRegistry(registry);
		}
#pragma warning restore ASPIRECOMPUTE003

		builder
			.AddYarp("yumney-gateway")
			.WithConfiguration(yarp =>
			{
				yarp.AddRoute("/api/v1/recipes/{**catch-all}", recipesApi);
				yarp.AddRoute("/api/v1/shopping-lists/{**catch-all}", shoppingApi);
				yarp.AddRoute("/api/v1/auth/{**catch-all}", usersApi);
				yarp.AddRoute("/api/v1/meal-plans/{**catch-all}", mealplanApi);
				yarp.AddRoute("/{**catch-all}", frontend.GetEndpoint("http"));
			})
			.WithExternalHttpEndpoints();

		frontend.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 1, 5, 100));
	}
}

builder.Build().Run();
