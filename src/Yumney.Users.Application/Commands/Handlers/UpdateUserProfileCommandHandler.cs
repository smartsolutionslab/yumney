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

		if (command.DisplayName is not null) profile.RenameAs(command.DisplayName);
		if (command.PreferredLanguage is not null) profile.SwitchLanguageTo(command.PreferredLanguage);
		if (command.PreferredUnitSystem is not null) profile.SwitchUnitSystemTo(command.PreferredUnitSystem);
		if (command.Theme is not null) profile.SwitchThemeTo(command.Theme);
		if (command.VoiceSettings is not null) profile.UpdateVoiceSettings(command.VoiceSettings);
		if (command.NotificationPreferences is not null) profile.UpdateNotificationPreferences(command.NotificationPreferences);
		profile.AdjustDefaultServingsTo(command.DefaultServings);
		profile.UpdateDietaryProfile(command.DietaryProfile);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return profile.ToDto(currentUser.Email);
	}
}
