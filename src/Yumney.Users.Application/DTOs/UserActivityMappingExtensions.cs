using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public static class UserActivityMappingExtensions
{
    extension(UserActivity activity)
    {
        public UserActivityDto ToDto()
        {
            return new UserActivityDto(
                activity.Type.Value,
                activity.RecipeIdentifier,
                activity.RecipeTitle,
                activity.OccurredAt);
        }
    }
}
