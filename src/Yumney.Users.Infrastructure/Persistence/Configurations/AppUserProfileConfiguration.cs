using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfile>
{
	public void Configure(EntityTypeBuilder<AppUserProfile> builder)
	{
		builder.ToTable("AppUserProfiles");
		builder.HasKey(profile => profile.Id);
		builder.Property(profile => profile.Id)
			.HasConversion<AppUserProfileIdentifierConverter>();

		builder.Property(profile => profile.KeycloakUserId)
			.HasConversion<KeycloakUserIdConverter>()
			.HasMaxLength(KeycloakUserId.MaxLength)
			.IsRequired();

		builder.Property(profile => profile.DisplayName)
			.HasConversion<DisplayNameConverter>()
			.HasMaxLength(DisplayName.MaxLength)
			.IsRequired();

		builder.Property(profile => profile.PreferredLanguage)
			.HasConversion<PreferredLanguageConverter>()
			.HasMaxLength(PreferredLanguage.MaxLength)
			.IsRequired();

		builder.Property(profile => profile.PreferredUnitSystem)
			.HasConversion<PreferredUnitSystemConverter>()
			.HasMaxLength(PreferredUnitSystem.MaxLength)
			.IsRequired();

		builder.Property(profile => profile.DefaultServings)
			.HasConversion<DefaultServingsConverter>()
			.HasDefaultValue(DefaultServings.Default)
			.IsRequired();

		builder.Property(profile => profile.Theme)
			.HasConversion<ThemeConverter>()
			.HasMaxLength(Theme.MaxLength)
			.HasDefaultValue(Theme.System)
			.IsRequired();

		builder.OwnsOne(profile => profile.VoiceSettings, voice =>
		{
			voice.Property(settings => settings.Enabled).HasColumnName("VoiceEnabled").HasDefaultValue(true);
			voice.Property(settings => settings.Speed)
				.HasConversion<VoiceSpeedConverter>()
				.HasMaxLength(VoiceSpeed.MaxLength)
				.HasColumnName("VoiceSpeed")
				.HasDefaultValue(VoiceSpeed.Normal);
			voice.Property(settings => settings.AutoReadInCookMode)
				.HasColumnName("VoiceAutoReadInCookMode")
				.HasDefaultValue(false);
		});

		builder.OwnsOne(profile => profile.NotificationPreferences, notifications =>
		{
			notifications.Property(prefs => prefs.TimerHapticFeedback)
				.HasColumnName("TimerHapticFeedback")
				.HasDefaultValue(true);
			notifications.Property(prefs => prefs.TimerSoundAlerts)
				.HasColumnName("TimerSoundAlerts")
				.HasDefaultValue(true);
		});

		builder.OwnsOne(profile => profile.DietaryProfile, dietary =>
		{
			dietary.Property(profile => profile.DietaryType)
				.HasConversion<DietaryTypeConverter>()
				.HasMaxLength(DietaryType.MaxLength)
				.HasColumnName("DietaryType");

			dietary.Property(profile => profile.Restrictions)
				.HasConversion<DietaryRestrictionsConverter>()
				.HasMaxLength(500)
				.HasColumnName("DietaryRestrictions");

			dietary.Property(profile => profile.CookingEffort)
				.HasConversion<CookingEffortPreferenceConverter>()
				.HasMaxLength(CookingEffortPreference.MaxLength)
				.HasColumnName("CookingEffort");

			dietary.OwnsOne(profile => profile.BalanceGoals, goals =>
			{
				goals.Property(balance => balance.MinVeggieMeals)
					.HasColumnName("MinVeggieMeals");

				goals.Property(balance => balance.MaxRedMeatMeals)
					.HasColumnName("MaxRedMeatMeals");
			});
		});

		builder.HasIndex(profile => profile.KeycloakUserId).IsUnique();
		builder.Ignore(profile => profile.DomainEvents);
	}
}
