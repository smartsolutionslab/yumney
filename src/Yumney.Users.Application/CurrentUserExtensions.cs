using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application;

public static class CurrentUserExtensions
{
    public static OwnerIdentifier AsOwner(this ICurrentUser currentUser) =>
        OwnerIdentifier.From(currentUser.UserId);
}
