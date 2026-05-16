using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.MigrationRunner;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
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

builder.Services.AddDbContext<MealPlanDbContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("mealplandb"),
		x => x.MigrationsHistoryTable("__MealPlanMigrationsHistory")));

// Minimal service registrations for the optional Shopping projection-rebuild path.
// The dashboard reset entry sets Persistence:RebuildShoppingProjections=true to drive
// MigrationWorker through this code path; the regular migration run never resolves them.
builder.Services.AddScoped<ShoppingListProjection>();
builder.Services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();

builder.Services.Configure<PersistenceOptions>(builder.Configuration.GetSection(PersistenceOptions.SectionName));

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

host.Run();
