using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

		// Drop the _http endpoint-name prefix: Aspire 13.3.3 may publish Keycloak
		// under the "https" endpoint name in dev (HTTPS upgrade), and the prefix
		// would only match an "http"-named endpoint. The bare service name lets
		// service discovery pick whichever scheme is published.
		services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
		{
			client.BaseAddress = new Uri("https+http://keycloak");
		})
			.ConfigurePrimaryHttpMessageHandler(sp =>
			{
				var handler = new HttpClientHandler();
				if (sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
				{
					// Aspire dev cert is self-signed; admin calls would otherwise fail TLS.
					handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
				}

				return handler;
			})
			.AddStandardResilienceHandler();

		services.AddHealthChecks().AddDbContextCheck<UsersDbContext>("usersdb");

		return services;
	}
}
