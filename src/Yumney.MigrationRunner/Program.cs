using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MigrationRunner;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<RecipesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("recipesdb"),
        x => x.MigrationsHistoryTable("__RecipesMigrationsHistory")));

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("usersdb"),
        x => x.MigrationsHistoryTable("__UsersMigrationsHistory")));

builder.Services.AddDbContext<ShoppingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("shoppingdb"),
        x => x.MigrationsHistoryTable("__ShoppingMigrationsHistory")));

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

host.Run();
