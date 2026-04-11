using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure;

public static class MealPlanInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMealPlanInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MealPlanDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("mealplandb");
            Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
                .MigrationsHistoryTable("__MealPlanMigrationsHistory")
                .EnableRetryOnFailure();

            options
                .UseNpgsql(connectionString, contextOptions)
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
        services.AddHealthChecks().AddDbContextCheck<MealPlanDbContext>("mealplandb");

        return services;
    }
}
