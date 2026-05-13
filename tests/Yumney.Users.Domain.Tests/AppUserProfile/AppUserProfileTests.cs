using FluentAssertions;
using SmartSolutionsLab.Yumney.TestBuilders.Users;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class AppUserProfileTests
{
	[Fact]
	public void Create_ValidParameters_ReturnsProfileWithCorrectValues()
	{
		var profile = AppUserProfileBuilder.A()
			.WithKeycloakUserId("kc-user-123")
			.Named("Test User")
			.Build();

		profile.KeycloakUserId.Should().Be(KeycloakUserId.From("kc-user-123"));
		profile.DisplayName.Should().Be(DisplayName.From("Test User"));
	}

	[Fact]
	public void Create_ValidParameters_GeneratesNonEmptyId()
	{
		var profile = AppUserProfileBuilder.A().Build();

		profile.Id.Should().NotBeNull();
	}

	[Fact]
	public void Create_ValidParameters_SetsDefaultPreferredLanguage()
	{
		var profile = AppUserProfileBuilder.A().Build();

		profile.PreferredLanguage.Should().Be(PreferredLanguage.From("en"));
	}

	[Fact]
	public void Create_ValidParameters_SetsDefaultPreferredUnitSystem()
	{
		var profile = AppUserProfileBuilder.A().Build();

		profile.PreferredUnitSystem.Should().Be(PreferredUnitSystem.From("metric"));
	}

	[Fact]
	public void Create_CalledTwice_GeneratesDifferentIds()
	{
		var profile1 = AppUserProfileBuilder.A().Build();
		var profile2 = AppUserProfileBuilder.A().Build();

		profile1.Id.Should().NotBe(profile2.Id);
	}

	[Fact]
	public void RenameAs_NewDisplayName_UpdatesDisplayName()
	{
		var profile = AppUserProfileBuilder.A().Build();
		var newName = DisplayName.From("New Name");

		profile.RenameAs(newName);

		profile.DisplayName.Should().Be(newName);
	}

	[Fact]
	public void SwitchLanguageTo_NewLanguage_UpdatesLanguage()
	{
		var profile = AppUserProfileBuilder.A().Build();
		var german = PreferredLanguage.From("de");

		profile.SwitchLanguageTo(german);

		profile.PreferredLanguage.Should().Be(german);
	}

	[Fact]
	public void SwitchUnitSystemTo_NewUnitSystem_UpdatesUnitSystem()
	{
		var profile = AppUserProfileBuilder.A().Build();
		var imperial = PreferredUnitSystem.From("imperial");

		profile.SwitchUnitSystemTo(imperial);

		profile.PreferredUnitSystem.Should().Be(imperial);
	}

	[Fact]
	public void Create_ValidParameters_SetsDefaultServingsTo4()
	{
		var profile = AppUserProfileBuilder.A().Build();

		profile.DefaultServings.Should().Be(DefaultServings.Default);
		profile.DefaultServings.Value.Should().Be(4);
	}

	[Fact]
	public void AdjustDefaultServingsTo_NewValue_UpdatesDefaultServings()
	{
		var profile = AppUserProfileBuilder.A().Build();
		var newServings = DefaultServings.From(6);

		profile.AdjustDefaultServingsTo(newServings);

		profile.DefaultServings.Should().Be(newServings);
	}

	[Fact]
	public void Create_ValidParameters_SetsEmptyDietaryProfile()
	{
		var profile = AppUserProfileBuilder.A().Build();

		profile.DietaryProfile.Should().Be(DietaryProfile.Empty);
		profile.DietaryProfile.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void UpdateDietaryProfile_NewProfile_UpdatesDietaryProfile()
	{
		var profile = AppUserProfileBuilder.A().Build();
		var dietary = DietaryProfile.From(
			DietaryType.Vegetarian,
			[DietaryRestriction.GlutenFree],
			WeeklyBalanceGoals.From(3, 1),
			CookingEffortPreference.QuickWeekdays);

		profile.UpdateDietaryProfile(dietary);

		profile.DietaryProfile.DietaryType.Should().Be(DietaryType.Vegetarian);
		profile.DietaryProfile.Restrictions.Should().HaveCount(1);
		profile.DietaryProfile.BalanceGoals.MinVeggieMeals.Should().Be(3);
		profile.DietaryProfile.CookingEffort.Should().Be(CookingEffortPreference.QuickWeekdays);
	}
}
