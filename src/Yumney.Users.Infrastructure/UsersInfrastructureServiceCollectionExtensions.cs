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

		services.AddOptions<SmtpOptions>()
			.Bind(configuration.GetSection(SmtpOptions.SectionName));
		services.AddScoped<IAccountDeletionEmailSender, SmtpAccountDeletionEmailSender>();

		services.AddYumneyNpgsqlDbContextWithOutbox<UsersDbContext>(
			configuration,
			"usersdb",
			"__UsersMigrationsHistory",
			"wolverine_users",
			typeof(DomainEventDispatchInterceptor));

		// State-based handlers (DeleteAccount, RegisterUser, UpdateUserProfile,
		// EnsureUserProfile, ResendVerificationEmail) stage cross-module
		// integration events on the typed outbox (last AddScoped<IEventBus>
		// wins, so this overrides the default WolverineEventBus from
		// AddYumneyDefaults). UsersUnitOfWork.SaveChangesAsync calls
		// outbox.FlushOutgoingMessagesAsync after persisting the entity
		// changes, which is what actually delivers the staged messages —
		// UserAccountDeletedIntegrationEvent is GDPR-critical and must not
		// sit waiting on the polling relay.
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
