using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;

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

    public static IServiceCollection AddCqrsLoggingDecorators(this IServiceCollection services)
    {
        DecorateAll(services, typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
        DecorateAll(services, typeof(IQueryHandler<,>), typeof(LoggingQueryHandlerDecorator<,>));
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

    private static void DecorateAll(IServiceCollection services, Type openGenericInterface, Type openGenericDecorator)
    {
        var descriptors = services
            .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == openGenericInterface)
            .ToList();

        foreach (var descriptor in descriptors)
        {
            var serviceType = descriptor.ServiceType;
            var genericArgs = serviceType.GetGenericArguments();
            var decoratorType = openGenericDecorator.MakeGenericType(genericArgs);

            services.Remove(descriptor);

            // Re-register the original handler under its concrete type so the decorator can resolve it
            if (descriptor.ImplementationType is not null)
            {
                services.Add(new ServiceDescriptor(descriptor.ImplementationType, descriptor.ImplementationType, descriptor.Lifetime));
            }

            services.AddScoped(serviceType, sp =>
            {
                // Resolve the inner handler: either from concrete type or from factory
                object inner;
                if (descriptor.ImplementationType is not null)
                {
                    inner = sp.GetRequiredService(descriptor.ImplementationType);
                }
                else if (descriptor.ImplementationFactory is not null)
                {
                    inner = descriptor.ImplementationFactory(sp);
                }
                else
                {
                    inner = descriptor.ImplementationInstance!;
                }

                return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);
            });
        }
    }
}
