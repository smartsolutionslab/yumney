using Microsoft.EntityFrameworkCore;
using Yumney.Users.Application.Interfaces;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Infrastructure.Persistence;

public sealed class AppUserProfileRepository(UsersDbContext context) : IAppUserProfileRepository
{
    public async Task<AppUserProfile?> FindByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        return await context.AppUserProfiles.FirstOrDefaultAsync(p => p.KeycloakUserId == keycloakUserId, cancellationToken);
    }

    public async Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default)
    {
        await context.AppUserProfiles.AddAsync(profile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
