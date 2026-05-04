using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed class AppUserProfile : AggregateRoot<AppUserProfileIdentifier>
{
	public KeycloakUserId KeycloakUserId { get; private set; } = default!;

	public DisplayName DisplayName { get; private set; } = default!;

	public PreferredLanguage PreferredLanguage { get; private set; } = PreferredLanguage.From("en");

	public PreferredUnitSystem PreferredUnitSystem { get; private set; } = PreferredUnitSystem.From("metric");

	public DefaultServings DefaultServings { get; private set; } = DefaultServings.Default;

	public DietaryProfile DietaryProfile { get; private set; } = DietaryProfile.Empty;

	public Theme Theme { get; private set; } = Theme.System;

	public VoiceSettings VoiceSettings { get; private set; } = VoiceSettings.Default;

	public NotificationPreferences NotificationPreferences { get; private set; } = NotificationPreferences.Default;

	private AppUserProfile()
	{
	}

	public static AppUserProfile Create(KeycloakUserId keycloakUserId, DisplayName displayName)
	{
		return new AppUserProfile
		{
			Id = AppUserProfileIdentifier.New(),
			KeycloakUserId = keycloakUserId,
			DisplayName = displayName,
		};
	}

	public AppUserProfile SwitchLanguageTo(PreferredLanguage language)
	{
		PreferredLanguage = language;
		return this;
	}

	public AppUserProfile SwitchUnitSystemTo(PreferredUnitSystem unitSystem)
	{
		PreferredUnitSystem = unitSystem;
		return this;
	}

	public AppUserProfile RenameAs(DisplayName displayName)
	{
		DisplayName = displayName;
		return this;
	}

	public AppUserProfile AdjustDefaultServingsTo(DefaultServings defaultServings)
	{
		DefaultServings = defaultServings;
		return this;
	}

	public AppUserProfile UpdateDietaryProfile(DietaryProfile dietaryProfile)
	{
		DietaryProfile = dietaryProfile;
		return this;
	}

	public AppUserProfile SwitchThemeTo(Theme theme)
	{
		Theme = theme;
		return this;
	}

	public AppUserProfile UpdateVoiceSettings(VoiceSettings voiceSettings)
	{
		VoiceSettings = voiceSettings;
		return this;
	}

	public AppUserProfile UpdateNotificationPreferences(NotificationPreferences notificationPreferences)
	{
		NotificationPreferences = notificationPreferences;
		return this;
	}
}
