using Microsoft.EntityFrameworkCore;
using Serilog;
using Yumney.MigrationRunner;
using Yumney.Recipes.Infrastructure.Persistence;
using Yumney.ServiceDefaults;
using Yumney.Users.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configuration => configuration.ReadFrom.Configuration(builder.Configuration));

builder.AddServiceDefaults();

builder.Services.AddDbContext<RecipesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("yumneydb"),
        x => x.MigrationsHistoryTable("__RecipesMigrationsHistory")));

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("yumneydb"),
        x => x.MigrationsHistoryTable("__UsersMigrationsHistory")));

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

host.Run();
