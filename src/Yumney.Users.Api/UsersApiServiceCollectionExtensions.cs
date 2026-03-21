using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Users.Api.Requests;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class UsersApiServiceCollectionExtensions
{
    public static IServiceCollection AddUsersApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();

        return services;
    }
}
