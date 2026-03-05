var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("yumneydb");

var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Yumney_Api>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
