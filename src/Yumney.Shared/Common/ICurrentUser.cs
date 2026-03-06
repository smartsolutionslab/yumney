namespace Yumney.Shared.Common;

public interface ICurrentUser
{
    string UserId { get; }

    string Email { get; }

    string DisplayName { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
