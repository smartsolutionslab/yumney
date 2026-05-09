using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

		// DeleteAccount keeps the default WolverineEventBus from AddYumneyDefaults.
		// Wolverine's typed IDbContextOutbox<T> only flushes captured messages when
		// the handler calls outbox.SaveChangesAndFlushMessagesAsync — a plain
		// DbContext.SaveChangesAsync stages but never delivers, so cross-module
		// integration events (UserAccountDeletedIntegrationEvent is GDPR-critical
		// and must not silently disappear). Wolverine's PersistMessagesWithPostgresql
		// still gives the regular bus durable at-least-once delivery; closing the
		// strict publish-before-save dual-write hole here is a follow-up that needs
		// the handler to use the outbox API directly.
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
