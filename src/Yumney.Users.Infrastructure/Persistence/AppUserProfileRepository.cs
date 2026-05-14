using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class AppUserProfileRepository(UsersDbContext context) : IAppUserProfileRepository
{
	private readonly DbSet<AppUserProfile> profiles = context.AppUserProfiles;

	public async Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		return await profiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.KeycloakUserId == keycloakUserId, cancellationToken);
	}

	public async Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		return await profiles.FirstOrDefaultAsync(profile => profile.KeycloakUserId == keycloakUserId, cancellationToken)
			?? throw new EntityNotFoundException(nameof(AppUserProfile), keycloakUserId.Value);
	}

	public async Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default)
	{
		await profiles.AddAsync(profile, cancellationToken);
	}

	public async Task DeleteByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
	{
		var matches = await profiles
			.Where(profile => profile.KeycloakUserId == keycloakUserId)
			.ToListAsync(cancellationToken);
		profiles.RemoveRange(matches);
	}
}
