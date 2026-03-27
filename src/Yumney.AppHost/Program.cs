using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using Scalar.Aspire;
using SmartSolutionsLab.Yumney.AppHost;
using SmartSolutionsLab.Yumney.AppHost.Options;

var builder = DistributedApplication.CreateBuilder(args);
var isRunMode = builder.ExecutionContext.IsRunMode;
var options = AppHostOptions.FromConfiguration(builder.Configuration);

builder.AddAzureContainerAppEnvironment("cae");

var postgresUser = builder.AddParameter("PostgresUser");
var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(userName: postgresUser, password: postgresPassword)
    .RunAsContainer(pg =>
    {
        pg.WithDataVolume();
        pg.WithPgAdmin();
    });

var recipesDb = postgres.AddDatabase("recipesdb");
var shoppingDb = postgres.AddDatabase("shoppingdb");
var usersDb = postgres.AddDatabase("usersdb");

var migrationRunner = builder.AddProject<Projects.Yumney_MigrationRunner>("yumney-migrations")
    .WithReference(recipesDb).WithReference(shoppingDb).WithReference(usersDb)
    .WaitFor(recipesDb).WaitFor(shoppingDb).WaitFor(usersDb);

if (options.DatabaseOnly)
{
    builder.Build().Run();
    return;
}

// ── Infrastructure ── (data volumes only in dev — ACA breaks file permissions)
var redis = builder.AddRedis("redis");
var messaging = builder.AddRabbitMQ("messaging").WithManagementPlugin();
var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword);

if (isRunMode)
{
    redis.WithDataVolume();
    messaging.WithDataVolume();
    keycloak.WithDataVolume();
}

keycloak.WithRealmImport("Realms");

if (isRunMode)
{
    var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "latest")
        .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")
        .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");
    keycloak.WaitFor(mailpit);
}
else
{
    keycloak
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEndpoint("http", e => e.IsExternal = true);
}

var recipesApi = builder
    .AddProject<Projects.Yumney_Recipes_Api>("recipes-api")
    .AsYumneyApi(recipesDb, keycloak, redis, messaging, migrationRunner)
    .WithLlmProvider(builder, options);
var shoppingApi = builder
    .AddProject<Projects.Yumney_Shopping_Api>("shopping-api")
    .AsYumneyApi(shoppingDb, keycloak, redis, messaging, migrationRunner);
var usersApi = builder
    .AddProject<Projects.Yumney_Users_Api>("users-api")
    .AsYumneyApi(usersDb, keycloak, redis, messaging, migrationRunner);

// ── Container Registry (GHCR for CI/CD) ──
if (options.UseGhcr)
{
#pragma warning disable ASPIRECOMPUTE003
    var registry = builder.AddContainerRegistry("ghcr", options.RegistryEndpoint!, options.RegistryRepository!);
    migrationRunner.WithContainerRegistry(registry);
    recipesApi.WithContainerRegistry(registry);
    shoppingApi.WithContainerRegistry(registry);
    usersApi.WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003
}

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
                        concurrentRequests.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    },
                },
            },
        });
    }
}

recipesApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 1, 10, 20));
shoppingApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 5, 50));
usersApi.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 3, 50));
migrationRunner.PublishAsAzureContainerApp((i, a) => ConfigureContainerApp(i, a, 0, 1));

if (isRunMode)
{
    var scalar = builder.AddScalarApiReference("scalar");
    scalar.WithApiReference(recipesApi);
    scalar.WithApiReference(shoppingApi);
    scalar.WithApiReference(usersApi);

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
        .WithReference(recipesApi).WithReference(shoppingApi).WithReference(usersApi)
        .WithReference(shell).WithReference(keycloak)
        .WaitFor(recipesApi).WaitFor(shoppingApi).WaitFor(usersApi)
        .WaitFor(shell).WaitFor(recipesMfe).WaitFor(shoppingMfe).WaitFor(accountMfe);
}
else
{
    builder.AddYarp("yumney-gateway")
        .WithStaticFiles("../../client/dist/apps/shell/browser")
        .WithConfiguration(yarp =>
        {
            yarp.AddRoute("/api/v1/recipes/{**catch-all}", recipesApi);
            yarp.AddRoute("/api/v1/shopping-lists/{**catch-all}", shoppingApi);
            yarp.AddRoute("/api/v1/auth/{**catch-all}", usersApi);
        })
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
