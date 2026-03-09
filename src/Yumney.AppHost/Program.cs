var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "latest")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume()
    .WithRealmImport("Realms")
    .WaitFor(mailpit);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var yumneyDb = postgres.AddDatabase("yumneydb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

var yumneyApi = builder.AddProject<Projects.Yumney_Api>("yumney-api")
    .WithReference(keycloak)
    .WithReference(yumneyDb)
    .WithReference(redis)
    .WithReference(ollama)
    .WaitFor(keycloak)
    .WaitFor(yumneyDb)
    .WaitFor(redis)
    .WaitFor(ollama);

builder.AddProject<Projects.Yumney_Gateway>("yumney-gateway")
    .WithReference(yumneyApi)
    .WithReference(keycloak)
    .WaitFor(yumneyApi);

builder.Build().Run();
