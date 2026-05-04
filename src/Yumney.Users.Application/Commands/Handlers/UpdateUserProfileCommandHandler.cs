using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

public sealed class UpdateUserProfileCommandHandler(IUsersUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
	public async Task<Result<UserProfileDto>> HandleAsync(UpdateUserProfileCommand command, CancellationToken cancellationToken = default)
	{
		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		var profile = await unitOfWork.Profiles.GetByKeycloakUserIdAsync(keycloakId, cancellationToken);

		ApplyIdentity(profile, command);
		ApplyPreferences(profile, command);
		profile.AdjustDefaultServingsTo(DefaultServings.From(command.DefaultServings));
		profile.UpdateDietaryProfile(BuildDietaryProfile(command));

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return profile.ToDto(currentUser.Email);
	}

	private static void ApplyIdentity(AppUserProfile profile, UpdateUserProfileCommand command)
	{
		if (command.DisplayName is not null)
		{
			profile.RenameAs(DisplayName.From(command.DisplayName));
		}
	}

	private static void ApplyPreferences(AppUserProfile profile, UpdateUserProfileCommand command)
	{
		if (command.PreferredLanguage is not null)
		{
			profile.SwitchLanguageTo(PreferredLanguage.From(command.PreferredLanguage));
		}

		if (command.PreferredUnitSystem is not null)
		{
			profile.SwitchUnitSystemTo(PreferredUnitSystem.From(command.PreferredUnitSystem));
		}

		if (command.Theme is not null)
		{
			profile.SwitchThemeTo(Theme.From(command.Theme));
		}

		if (command.VoiceSettings is not null)
		{
			var (enabled, speed, autoRead) = command.VoiceSettings;
			profile.UpdateVoiceSettings(new VoiceSettings(enabled, VoiceSpeed.From(speed), autoRead));
		}

		if (command.NotificationPreferences is not null)
		{
			var (haptic, sound) = command.NotificationPreferences;
			profile.UpdateNotificationPreferences(new NotificationPreferences(haptic, sound));
		}
	}

	private static DietaryProfile BuildDietaryProfile(UpdateUserProfileCommand command)
	{
		var dietaryType = command.DietaryType is not null ? DietaryType.From(command.DietaryType) : null;
		var restrictions = command.Restrictions.Select(DietaryRestriction.From).ToList();
		var balanceGoals = WeeklyBalanceGoals.From(command.MinVeggieMeals, command.MaxRedMeatMeals);
		var cookingEffort = command.CookingEffort is not null ? CookingEffortPreference.From(command.CookingEffort) : null;

		return DietaryProfile.From(dietaryType, restrictions, balanceGoals, cookingEffort);
	}
}
