using Microsoft.Extensions.DependencyInjection;

namespace Yumney.Shared.Events;

public static class EventingServiceCollectionExtensions
{
    public static IServiceCollection AddInProcessEventBus(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        services.AddScoped<IEventBus, InProcessEventBus>();

        return services;
    }
}
