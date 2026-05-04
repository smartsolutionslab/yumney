using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfile>
{
	public void Configure(EntityTypeBuilder<AppUserProfile> entity)
	{
		entity.ToTable("AppUserProfiles");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.Id)
			.HasConversion<AppUserProfileIdentifierConverter>();

		entity.Property(e => e.KeycloakUserId)
			.HasConversion<KeycloakUserIdConverter>()
			.HasMaxLength(KeycloakUserId.MaxLength)
			.IsRequired();

		entity.Property(e => e.DisplayName)
			.HasConversion<DisplayNameConverter>()
			.HasMaxLength(DisplayName.MaxLength)
			.IsRequired();

		entity.Property(e => e.PreferredLanguage)
			.HasConversion<PreferredLanguageConverter>()
			.HasMaxLength(PreferredLanguage.MaxLength)
			.IsRequired();

		entity.Property(e => e.PreferredUnitSystem)
			.HasConversion<PreferredUnitSystemConverter>()
			.HasMaxLength(PreferredUnitSystem.MaxLength)
			.IsRequired();

		entity.Property(e => e.DefaultServings)
			.HasConversion<DefaultServingsConverter>()
			.HasDefaultValue(DefaultServings.Default)
			.IsRequired();

		entity.Property(e => e.Theme)
			.HasConversion<ThemeConverter>()
			.HasMaxLength(Theme.MaxLength)
			.HasDefaultValue(Theme.System)
			.IsRequired();

		entity.OwnsOne(e => e.VoiceSettings, voice =>
		{
			voice.Property(v => v.Enabled).HasColumnName("VoiceEnabled").HasDefaultValue(true);
			voice.Property(v => v.Speed)
				.HasConversion<VoiceSpeedConverter>()
				.HasMaxLength(VoiceSpeed.MaxLength)
				.HasColumnName("VoiceSpeed")
				.HasDefaultValue(VoiceSpeed.Normal);
			voice.Property(v => v.AutoReadInCookMode)
				.HasColumnName("VoiceAutoReadInCookMode")
				.HasDefaultValue(false);
		});

		entity.OwnsOne(e => e.NotificationPreferences, notifications =>
		{
			notifications.Property(n => n.TimerHapticFeedback)
				.HasColumnName("TimerHapticFeedback")
				.HasDefaultValue(true);
			notifications.Property(n => n.TimerSoundAlerts)
				.HasColumnName("TimerSoundAlerts")
				.HasDefaultValue(true);
		});

		entity.OwnsOne(e => e.DietaryProfile, dietary =>
		{
			dietary.Property(d => d.DietaryType)
				.HasConversion<DietaryTypeConverter>()
				.HasMaxLength(DietaryType.MaxLength)
				.HasColumnName("DietaryType");

			dietary.Property(d => d.Restrictions)
				.HasConversion<DietaryRestrictionsConverter>()
				.HasMaxLength(500)
				.HasColumnName("DietaryRestrictions");

			dietary.Property(d => d.CookingEffort)
				.HasConversion<CookingEffortPreferenceConverter>()
				.HasMaxLength(CookingEffortPreference.MaxLength)
				.HasColumnName("CookingEffort");

			dietary.OwnsOne(d => d.BalanceGoals, goals =>
			{
				goals.Property(g => g.MinVeggieMeals)
					.HasColumnName("MinVeggieMeals");

				goals.Property(g => g.MaxRedMeatMeals)
					.HasColumnName("MaxRedMeatMeals");
			});
		});

		entity.HasIndex(e => e.KeycloakUserId).IsUnique();
		entity.Ignore(e => e.DomainEvents);
	}
}
