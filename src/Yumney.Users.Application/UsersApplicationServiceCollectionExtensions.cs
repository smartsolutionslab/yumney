using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application;

public static class UsersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<ResendVerificationEmailCommand, Result>, ResendVerificationEmailCommandHandler>();

        return services;
    }
}
