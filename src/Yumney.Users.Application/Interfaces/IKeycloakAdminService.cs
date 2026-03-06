using Yumney.Shared.Common;

namespace Yumney.Users.Application.Interfaces;

public interface IKeycloakAdminService
{
    Task<Result<string>> CreateUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<Result<string>> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Result> SendVerificationEmailAsync(
        string keycloakUserId,
        CancellationToken cancellationToken = default);
}
