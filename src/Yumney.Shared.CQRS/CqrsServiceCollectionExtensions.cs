using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.CQRS;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddHandlersFromAssemblyContaining<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;

        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type openGenericInterface)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                .Select(i => (Service: i, Implementation: t)));

        foreach (var (service, implementation) in handlerTypes)
        {
            services.AddScoped(service, implementation);
        }
    }
}
