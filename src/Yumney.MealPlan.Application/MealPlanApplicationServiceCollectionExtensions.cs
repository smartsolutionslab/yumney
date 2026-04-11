using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application;

public static class MealPlanApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMealPlanApplication(this IServiceCollection services)
    {
        services.AddHandlersFromAssemblyContaining<Queries.Handlers.GetWeeklyPlanQueryHandler>();
        return services;
    }
}
