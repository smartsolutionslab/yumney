using Azure.Provisioning.AppContainers;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment
builder.AddAzureContainerAppEnvironment("cae");

// Parameters for Azure deployment
var postgresUser = builder.AddParameter("PostgresUser");
var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true);

// PostgreSQL — Azure Flexible Server in prod, container in dev
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

// Redis
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// RabbitMQ
var messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

// Keycloak
var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword)
    .WithDataVolume();

if (builder.ExecutionContext.IsRunMode)
{
    var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "latest")
        .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")
        .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

    keycloak
        .WithRealmImport("Realms")
        .WaitFor(mailpit);
}
else
{
    keycloak
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEndpoint("http", e => e.IsExternal = true);
}

// LLM Provider — configurable via appsettings.json ("Ollama" or "OpenAI")
var llmProvider = builder.Configuration.GetValue<string>("LlmProvider") ?? "Ollama";
var useOllama = llmProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase);

IResourceBuilder<OllamaResource>? ollama = null;
IResourceBuilder<ParameterResource>? openAiApiKey = null;

if (useOllama)
{
    ollama = builder.AddOllama("ollama")
        .WithDataVolume();
}
else
{
    openAiApiKey = builder.AddParameter("OpenAiApiKey", secret: true);
}

// Migration Runner — single instance, runs once then exits
var migrationRunner = builder.AddProject<Projects.Yumney_MigrationRunner>("yumney-migrations")
    .WithReference(recipesDb)
    .WithReference(shoppingDb)
    .WithReference(usersDb)
    .WaitFor(recipesDb)
    .WaitFor(shoppingDb)
    .WaitFor(usersDb);

migrationRunner.PublishAsAzureContainerApp((infra, app) =>
{
    app.Template.Scale.MinReplicas = 0;
    app.Template.Scale.MaxReplicas = 1;
});

// Recipes API (needs keycloak, recipesdb, redis, LLM provider)
var recipesApi = builder.AddProject<Projects.Yumney_Recipes_Api>("recipes-api")
    .WithHttpEndpoint()
    .WithReference(keycloak)
    .WithReference(recipesDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WaitFor(keycloak)
    .WaitFor(migrationRunner)
    .WaitFor(redis)
    .WaitFor(messaging)
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar";
        url.Url = "/scalar/v1";
    });

if (useOllama)
{
    recipesApi
        .WithReference(ollama!)
        .WaitFor(ollama!);
}
else
{
    var openAiModel = builder.Configuration.GetValue<string>("OpenAi:ModelId") ?? "gpt-5.3-chat-latest";

    recipesApi
        .WithEnvironment("SemanticKernel__Provider", "OpenAI")
        .WithEnvironment("SemanticKernel__ModelId", openAiModel)
        .WithEnvironment("SemanticKernel__ApiKey", openAiApiKey!);
}

// Shopping API (needs keycloak, shoppingdb, redis)
var shoppingApi = builder.AddProject<Projects.Yumney_Shopping_Api>("shopping-api")
    .WithHttpEndpoint()
    .WithReference(keycloak)
    .WithReference(shoppingDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WaitFor(keycloak)
    .WaitFor(migrationRunner)
    .WaitFor(redis)
    .WaitFor(messaging)
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar";
        url.Url = "/scalar/v1";
    });

// Users API (needs keycloak, usersdb, redis)
var usersApi = builder.AddProject<Projects.Yumney_Users_Api>("users-api")
    .WithHttpEndpoint()
    .WithReference(keycloak)
    .WithReference(usersDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WaitFor(keycloak)
    .WaitFor(migrationRunner)
    .WaitFor(redis)
    .WaitFor(messaging)
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar";
        url.Url = "/scalar/v1";
    });

// Container images — use pre-built GHCR images when ImagePrefix is configured (CI/CD),
// otherwise aspire deploy builds images and pushes to the auto-provisioned ACR.
var imagePrefix = builder.Configuration.GetValue<string>("ImagePrefix") ?? "";
var imageTag = builder.Configuration.GetValue<string>("ImageTag") ?? "latest";

if (!string.IsNullOrEmpty(imagePrefix))
{
#pragma warning disable ASPIREPIPELINES003
    migrationRunner
        .WithRemoteImageName($"{imagePrefix}/migration-runner")
        .WithRemoteImageTag(imageTag);
    recipesApi
        .WithRemoteImageName($"{imagePrefix}/recipes-api")
        .WithRemoteImageTag(imageTag);
    shoppingApi
        .WithRemoteImageName($"{imagePrefix}/shopping-api")
        .WithRemoteImageTag(imageTag);
    usersApi
        .WithRemoteImageName($"{imagePrefix}/users-api")
        .WithRemoteImageTag(imageTag);
#pragma warning restore ASPIREPIPELINES003
}

// Azure Container Apps — scaling configuration
recipesApi.PublishAsAzureContainerApp((infra, app) =>
{
    app.Template.Scale.MinReplicas = 1;
    app.Template.Scale.MaxReplicas = 10;
    app.Template.Scale.Rules.Add(new ContainerAppScaleRule
    {
        Name = "http-scaling",
        Http = new ContainerAppHttpScaleRule
        {
            Metadata = { { "concurrentRequests", "20" }, },
        },
    });
});

shoppingApi.PublishAsAzureContainerApp((infra, app) =>
{
    app.Template.Scale.MinReplicas = 0;
    app.Template.Scale.MaxReplicas = 5;
    app.Template.Scale.Rules.Add(new ContainerAppScaleRule
    {
        Name = "http-scaling",
        Http = new ContainerAppHttpScaleRule
        {
            Metadata = { { "concurrentRequests", "50" }, },
        },
    });
});

usersApi.PublishAsAzureContainerApp((infra, app) =>
{
    app.Template.Scale.MinReplicas = 0;
    app.Template.Scale.MaxReplicas = 3;
    app.Template.Scale.Rules.Add(new ContainerAppScaleRule
    {
        Name = "http-scaling",
        Http = new ContainerAppHttpScaleRule
        {
            Metadata = { { "concurrentRequests", "50" }, },
        },
    });
});

// Frontend Micro-Frontends + Gateway
if (builder.ExecutionContext.IsRunMode)
{
    var shell = builder.AddJavaScriptApp("shell", "../../client", "serve:shell")
        .WithYarn()
        .WithHttpEndpoint(targetPort: 4200);

    var recipesMfe = builder.AddJavaScriptApp("recipes-mfe", "../../client", "serve:recipes")
        .WithYarn()
        .WithHttpEndpoint(targetPort: 4201);

    var shoppingMfe = builder.AddJavaScriptApp("shopping-mfe", "../../client", "serve:shopping")
        .WithYarn()
        .WithHttpEndpoint(targetPort: 4202);

    var accountMfe = builder.AddJavaScriptApp("account-mfe", "../../client", "serve:account")
        .WithYarn()
        .WithHttpEndpoint(targetPort: 4203);

    builder.AddProject<Projects.Yumney_Gateway>("yumney-gateway")
        .WithHttpEndpoint(port: 5100)
        .WithReference(recipesApi)
        .WithReference(shoppingApi)
        .WithReference(usersApi)
        .WithReference(shell)
        .WithReference(keycloak)
        .WaitFor(recipesApi)
        .WaitFor(shoppingApi)
        .WaitFor(usersApi)
        .WaitFor(shell)
        .WaitFor(recipesMfe)
        .WaitFor(shoppingMfe)
        .WaitFor(accountMfe);
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
