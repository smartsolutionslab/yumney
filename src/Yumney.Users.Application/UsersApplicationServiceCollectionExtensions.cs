using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Commands;

namespace SmartSolutionsLab.Yumney.Users.Application;

public static class UsersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();

        services.AddScoped<ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<ResendVerificationEmailCommand, Result>, ResendVerificationEmailCommandHandler>();

        return services;
    }
}
