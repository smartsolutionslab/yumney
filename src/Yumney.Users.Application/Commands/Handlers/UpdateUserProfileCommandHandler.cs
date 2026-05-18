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
		var (displayName, preferredLanguage, preferredUnitSystem, defaultServings, theme, voiceSettings, notificationPreferences, dietaryProfile) = command;

		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		var profile = await unitOfWork.Profiles.GetByKeycloakUserIdAsync(keycloakId, cancellationToken);

		if (displayName is not null) profile.RenameAs(displayName);
		if (preferredLanguage is not null) profile.SwitchLanguageTo(preferredLanguage);
		if (preferredUnitSystem is not null) profile.SwitchUnitSystemTo(preferredUnitSystem);
		if (theme is not null) profile.SwitchThemeTo(theme);
		if (voiceSettings is not null) profile.UpdateVoiceSettings(voiceSettings);
		if (notificationPreferences is not null) profile.UpdateNotificationPreferences(notificationPreferences);
		profile.AdjustDefaultServingsTo(defaultServings);
		profile.UpdateDietaryProfile(dietaryProfile);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return profile.ToDto(currentUser.Email);
	}
}
