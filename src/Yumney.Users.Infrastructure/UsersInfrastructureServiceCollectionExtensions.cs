using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public static class UsersInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<KeycloakOptions>()
			.Bind(configuration.GetSection(KeycloakOptions.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddDbContext<UsersDbContext>((sp, options) =>
		{
			var connectionString = configuration.GetConnectionString("usersdb");
			options
				.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__UsersMigrationsHistory").EnableRetryOnFailure())
				.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
		});

		services.AddScoped<IAppUserProfileRepository, AppUserProfileRepository>();
		services.AddScoped<IUserActivityRepository, UserActivityRepository>();
		services.AddScoped<IStaplesListRepository, StaplesListRepository>();
		services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
		services.AddScoped<IStaplesProvider, StaplesProvider>();

		services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
		{
			client.BaseAddress = new Uri("https+http://_http.keycloak");
		}).AddStandardResilienceHandler();

		services.AddHealthChecks().AddDbContextCheck<UsersDbContext>("usersdb");

		return services;
	}
}
