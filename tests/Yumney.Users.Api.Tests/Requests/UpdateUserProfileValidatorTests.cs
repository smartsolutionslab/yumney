using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Api.Tests.Requests;

public class UpdateUserProfileValidatorTests
{
	private readonly Api.Requests.UpdateUserProfileValidator validator = new();

	[Fact]
	public void Validate_MinimalValidRequest_IsValid()
	{
		var request = MinimalRequest();

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_FullyPopulatedValidRequest_IsValid()
	{
		var request = MinimalRequest() with
		{
			DisplayName = "Renamed",
			PreferredLanguage = "de",
			PreferredUnitSystem = "imperial",
			Theme = "dark",
			VoiceSettings = new VoiceSettingsDto(false, "fast", true),
			NotificationPreferences = new NotificationPreferencesDto(false, false),
			DietaryType = "vegetarian",
			Restrictions = ["gluten-free"],
			MinVeggieMeals = 3,
			MaxRedMeatMeals = 2,
			CookingEffort = "balanced",
		};

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(13)]
	public void Validate_DefaultServingsOutOfRange_IsNotValid(int servings)
	{
		var request = MinimalRequest() with { DefaultServings = servings };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(Api.Requests.UpdateUserProfile.DefaultServings));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(8)]
	public void Validate_MinVeggieMealsOutOfRange_IsNotValid(int minVeggieMeals)
	{
		var request = MinimalRequest() with { MinVeggieMeals = minVeggieMeals };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName.Contains("MinVeggieMeals"));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(8)]
	public void Validate_MaxRedMeatMealsOutOfRange_IsNotValid(int maxRedMeatMeals)
	{
		var request = MinimalRequest() with { MaxRedMeatMeals = maxRedMeatMeals };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName.Contains("MaxRedMeatMeals"));
	}

	[Fact]
	public void Validate_DisplayNameTooLong_IsNotValid()
	{
		var request = MinimalRequest() with { DisplayName = new string('x', 201) };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(Api.Requests.UpdateUserProfile.DisplayName));
	}

	[Fact]
	public void Validate_NullOptionalsAreAllowed()
	{
		var request = MinimalRequest();

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	private static Api.Requests.UpdateUserProfile MinimalRequest() =>
		new(
			DisplayName: null,
			PreferredLanguage: null,
			PreferredUnitSystem: null,
			DefaultServings: 4,
			Theme: null,
			VoiceSettings: null,
			NotificationPreferences: null,
			DietaryType: null,
			Restrictions: [],
			MinVeggieMeals: null,
			MaxRedMeatMeals: null,
			CookingEffort: null);
}
