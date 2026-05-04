using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class UpdateUserProfileCommandHandlerTests
{
	private readonly IAppUserProfileRepository profiles = Substitute.For<IAppUserProfileRepository>();
	private readonly IUsersUnitOfWork unitOfWork = Substitute.For<IUsersUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly UpdateUserProfileCommandHandler handler;

	public UpdateUserProfileCommandHandlerTests()
	{
		currentUser.UserId.Returns("kc-user-123");
		currentUser.Email.Returns("test@example.com");
		unitOfWork.Profiles.Returns(profiles);
		handler = new UpdateUserProfileCommandHandler(unitOfWork, currentUser);
	}

	[Fact]
	public async Task HandleAsync_UpdatesDefaultServings()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(defaultServings: 6));

		result.IsSuccess.Should().BeTrue();
		result.Value.DefaultServings.Should().Be(6);
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_UpdatesDietaryType()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(dietaryType: "vegetarian"));

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.DietaryType.Should().Be("vegetarian");
	}

	[Fact]
	public async Task HandleAsync_UpdatesRestrictions()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(restrictions: ["gluten-free", "nut-allergy"]));

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.Restrictions.Should().BeEquivalentTo(["gluten-free", "nut-allergy"]);
	}

	[Fact]
	public async Task HandleAsync_UpdatesWeeklyBalanceGoals()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(minVeggieMeals: 3, maxRedMeatMeals: 2));

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.MinVeggieMeals.Should().Be(3);
		result.Value.DietaryProfile.MaxRedMeatMeals.Should().Be(2);
	}

	[Fact]
	public async Task HandleAsync_UpdatesCookingEffort()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(cookingEffort: "quick-weekdays"));

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.CookingEffort.Should().Be("quick-weekdays");
	}

	[Fact]
	public async Task HandleAsync_ReturnsMappedDto()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command());

		result.IsSuccess.Should().BeTrue();
		result.Value.DisplayName.Should().Be("Test User");
		result.Value.Email.Should().Be("test@example.com");
		result.Value.PreferredLanguage.Should().Be("en");
		result.Value.PreferredUnitSystem.Should().Be("metric");
	}

	[Fact]
	public async Task HandleAsync_UpdatesDisplayName()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(displayName: "Renamed"));

		result.Value.DisplayName.Should().Be("Renamed");
	}

	[Fact]
	public async Task HandleAsync_UpdatesPreferredLanguage()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(preferredLanguage: "de"));

		result.Value.PreferredLanguage.Should().Be("de");
	}

	[Fact]
	public async Task HandleAsync_UpdatesPreferredUnitSystem()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(preferredUnitSystem: "imperial"));

		result.Value.PreferredUnitSystem.Should().Be("imperial");
	}

	[Fact]
	public async Task HandleAsync_UpdatesTheme()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(theme: "dark"));

		result.Value.Theme.Should().Be("dark");
	}

	[Fact]
	public async Task HandleAsync_UpdatesVoiceSettings()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(
			voiceSettings: new VoiceSettingsDto(false, "fast", true)));

		result.Value.VoiceSettings.Enabled.Should().BeFalse();
		result.Value.VoiceSettings.Speed.Should().Be("fast");
		result.Value.VoiceSettings.AutoReadInCookMode.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_UpdatesNotificationPreferences()
	{
		CreateProfile();

		var result = await handler.HandleAsync(Command(
			notificationPreferences: new NotificationPreferencesDto(false, false)));

		result.Value.NotificationPreferences.TimerHapticFeedback.Should().BeFalse();
		result.Value.NotificationPreferences.TimerSoundAlerts.Should().BeFalse();
	}

	private static UpdateUserProfileCommand Command(
		string? displayName = null,
		string? preferredLanguage = null,
		string? preferredUnitSystem = null,
		int defaultServings = 4,
		string? theme = null,
		VoiceSettingsDto? voiceSettings = null,
		NotificationPreferencesDto? notificationPreferences = null,
		string? dietaryType = null,
		IReadOnlyList<string>? restrictions = null,
		int? minVeggieMeals = null,
		int? maxRedMeatMeals = null,
		string? cookingEffort = null) =>
		new(
			displayName,
			preferredLanguage,
			preferredUnitSystem,
			defaultServings,
			theme,
			voiceSettings,
			notificationPreferences,
			dietaryType,
			restrictions ?? [],
			minVeggieMeals,
			maxRedMeatMeals,
			cookingEffort);

	private AppUserProfile CreateProfile()
	{
		var profile = AppUserProfile.Create(
			KeycloakUserId.From("kc-user-123"),
			DisplayName.From("Test User"));

		profiles.GetByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(profile);

		return profile;
	}
}
