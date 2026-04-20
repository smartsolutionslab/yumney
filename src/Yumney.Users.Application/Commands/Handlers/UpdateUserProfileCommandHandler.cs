using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class UpdateUserProfileCommandHandler(IUsersUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
	public async Task<Result<UserProfileDto>> HandleAsync(UpdateUserProfileCommand command, CancellationToken cancellationToken = default)
	{
		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		var profile = await unitOfWork.Profiles.GetByKeycloakUserIdAsync(keycloakId, cancellationToken);

		profile.AdjustDefaultServingsTo(DefaultServings.From(command.DefaultServings));
		profile.UpdateDietaryProfile(BuildDietaryProfile(command));

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return profile.ToDto();
	}

	private static DietaryProfile BuildDietaryProfile(UpdateUserProfileCommand command)
	{
		var (_, dietaryTypeValue, restrictionValues, minVeggieMeals, maxRedMeatMeals, cookingEffortValue) = command;

		var dietaryType = dietaryTypeValue is not null ? DietaryType.From(dietaryTypeValue) : null;
		var restrictions = restrictionValues.Select(DietaryRestriction.From).ToList();
		var balanceGoals = WeeklyBalanceGoals.From(minVeggieMeals, maxRedMeatMeals);
		var cookingEffort = cookingEffortValue is not null ? CookingEffortPreference.From(cookingEffortValue) : null;

		return DietaryProfile.From(dietaryType, restrictions, balanceGoals, cookingEffort);
	}
}
