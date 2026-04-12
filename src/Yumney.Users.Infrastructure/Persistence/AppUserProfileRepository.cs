using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class AppUserProfileRepository(UsersDbContext context) : IAppUserProfileRepository
{
    private readonly DbSet<AppUserProfile> profiles = context.AppUserProfiles;

    public async Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
    {
        return await profiles.AsNoTracking().FirstOrDefaultAsync(p => p.KeycloakUserId == keycloakUserId, cancellationToken);
    }

    public async Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default)
    {
        return await profiles.FirstOrDefaultAsync(p => p.KeycloakUserId == keycloakUserId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(AppUserProfile), keycloakUserId.Value);
    }

    public async Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default)
    {
        await profiles.AddAsync(profile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
