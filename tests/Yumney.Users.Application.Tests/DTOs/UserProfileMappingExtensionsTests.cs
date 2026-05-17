using FluentAssertions;
using SmartSolutionsLab.Yumney.TestBuilders.Users;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.DTOs;

public class UserProfileMappingExtensionsTests
{
	[Fact]
	public void Profile_ToDto_MapsAllFields()
	{
		var profile = AppUserProfileBuilder.A()
			.WithKeycloakUserId("kc-user-1")
			.Named("Alice")
			.Build();
		profile.SwitchLanguageTo(PreferredLanguage.From("de"));
		profile.SwitchUnitSystemTo(PreferredUnitSystem.From("imperial"));
		profile.AdjustDefaultServingsTo(DefaultServings.From(6));
		profile.SwitchThemeTo(Theme.Dark);

		var dto = profile.ToDto("alice@example.com");

		dto.DisplayName.Should().Be("Alice");
		dto.Email.Should().Be("alice@example.com");
		dto.PreferredLanguage.Should().Be("de");
		dto.PreferredUnitSystem.Should().Be("imperial");
		dto.DefaultServings.Should().Be(6);
		dto.Theme.Should().Be("dark");
		dto.VoiceSettings.Should().NotBeNull();
		dto.NotificationPreferences.Should().NotBeNull();
		dto.DietaryProfile.Should().NotBeNull();
	}

	[Fact]
	public void VoiceSettings_ToDto_MapsAllFields()
	{
		var settings = new VoiceSettings(Enabled: false, VoiceSpeed.Fast, AutoReadInCookMode: true);

		var dto = settings.ToDto();

		dto.Enabled.Should().BeFalse();
		dto.Speed.Should().Be("fast");
		dto.AutoReadInCookMode.Should().BeTrue();
	}

	[Fact]
	public void NotificationPreferences_ToDto_MapsAllFields()
	{
		var prefs = new NotificationPreferences(TimerHapticFeedback: true, TimerSoundAlerts: false);

		var dto = prefs.ToDto();

		dto.TimerHapticFeedback.Should().BeTrue();
		dto.TimerSoundAlerts.Should().BeFalse();
	}

	[Fact]
	public void DietaryProfile_ToDto_EmptyProfile_MapsNullsAndEmpties()
	{
		var dto = DietaryProfile.Empty.ToDto();

		dto.DietaryType.Should().BeNull();
		dto.Restrictions.Should().BeEmpty();
		dto.CookingEffort.Should().BeNull();
	}

	[Fact]
	public void DietaryProfile_ToDto_PopulatedProfile_MapsAllFields()
	{
		var dietary = DietaryProfile.From(
			DietaryType.Vegan,
			[DietaryRestriction.GlutenFree, DietaryRestriction.NutAllergy],
			WeeklyBalanceGoals.From(5, 0),
			CookingEffortPreference.QuickWeekdays);

		var dto = dietary.ToDto();

		dto.DietaryType.Should().Be("vegan");
		dto.Restrictions.Should().BeEquivalentTo(["gluten-free", "nut-allergy"]);
		dto.MinVeggieMeals.Should().Be(5);
		dto.MaxRedMeatMeals.Should().Be(0);
		dto.CookingEffort.Should().Be("quick-weekdays");
	}
}
