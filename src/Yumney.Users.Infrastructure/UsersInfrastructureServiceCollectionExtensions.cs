using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public static class UsersInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("usersdb"),
                x => x.MigrationsHistoryTable("__UsersMigrationsHistory")));

        services.AddScoped<IAppUserProfileRepository, AppUserProfileRepository>();

        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
        {
            client.BaseAddress = new Uri("https+http://keycloak");
        }).AddStandardResilienceHandler();

        return services;
    }
}
