using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

namespace SmartSolutionsLab.Yumney.Users.Application;

public static class UsersApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddUsersApplication(this IServiceCollection services)
	{
		services.AddHandlersFromAssemblyContaining<RegisterUserCommandHandler>();
		services.AddBusEventHandlersFromAssemblyContaining<RegisterUserCommandHandler>();

		return services;
	}
}
