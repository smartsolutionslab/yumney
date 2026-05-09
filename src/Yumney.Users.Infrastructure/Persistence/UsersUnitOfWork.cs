using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersUnitOfWork(
	UsersDbContext context,
	IDbContextOutbox<UsersDbContext> outbox) : IUsersUnitOfWork
{
	public IAppUserProfileRepository Profiles => field ??= new AppUserProfileRepository(context);

	public IUserActivityRepository Activities => field ??= new UserActivityRepository(context);

	public IStaplesListRepository StaplesLists => field ??= new StaplesListRepository(context);

	// Save first so Wolverine's EF interceptor captures any messages staged via
	// IDbContextOutbox<UsersDbContext>.PublishAsync into the outbox table inside
	// the same Postgres transaction. FlushOutgoingMessagesAsync then nudges the
	// Wolverine relay to deliver immediately — UserAccountDeletedIntegrationEvent
	// is GDPR-critical and must not sit waiting on the polling relay. A flush
	// failure leaves the staged rows in the outbox for retry by the background
	// relay, preserving at-least-once.
	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var rowCount = await context.SaveChangesAsync(cancellationToken);
		await outbox.FlushOutgoingMessagesAsync();
		return rowCount;
	}
}
