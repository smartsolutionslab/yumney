using Yumney.Shared.Common;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Application.Interfaces;

public interface IKeycloakAdminService
{
    Task<Result<KeycloakUserId>> CreateUserAsync(Email email, Password password, DisplayName displayName, CancellationToken cancellationToken = default);

    Task<Result<KeycloakUserId>> FindUserByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<Result> SendVerificationEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);
}
