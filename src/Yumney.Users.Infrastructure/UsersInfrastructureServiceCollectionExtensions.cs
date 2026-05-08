using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
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

		services.AddYumneyNpgsqlDbContextWithOutbox<UsersDbContext>(
			configuration,
			"usersdb",
			"__UsersMigrationsHistory",
			"wolverine_users",
			typeof(DomainEventDispatchInterceptor));

		// Replace the default WolverineEventBus (registered by AddYumneyDefaults)
		// with the outbox-backed variant so DeleteAccount becomes transactional
		// — UserAccountDeletedIntegrationEvent is GDPR-critical, so a publish
		// failure that's not paired with the entity write is unacceptable.
		// Last AddScoped<IEventBus> wins, so this binding takes effect.
		services.AddScoped<IEventBus, WolverineOutboxEventBus<UsersDbContext>>();

		services.AddScoped<UsersUnitOfWork>();
		services.AddScoped<IUsersUnitOfWork>(sp => sp.GetRequiredService<UsersUnitOfWork>());
		services.AddScoped<IAppUserProfileRepository>(sp => sp.GetRequiredService<UsersUnitOfWork>().Profiles);
		services.AddScoped<IUserActivityRepository>(sp => sp.GetRequiredService<UsersUnitOfWork>().Activities);
		services.AddScoped<IStaplesListRepository>(sp => sp.GetRequiredService<UsersUnitOfWork>().StaplesLists);
		services.AddScoped<IStaplesProvider, StaplesProvider>();

		services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
		{
			client.BaseAddress = new Uri("https+http://_http.keycloak");
		}).AddStandardResilienceHandler();

		services.AddHealthChecks().AddDbContextCheck<UsersDbContext>("usersdb");

		return services;
	}
}
