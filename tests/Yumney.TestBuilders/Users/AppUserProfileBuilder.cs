using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using AppUserProfileAggregate = SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile.AppUserProfile;

namespace SmartSolutionsLab.Yumney.TestBuilders.Users;

public sealed class AppUserProfileBuilder
{
	private KeycloakUserId keycloakUserId = KeycloakUserId.From("kc-user-123");
	private DisplayName displayName = DisplayName.From("Test User");
	private PreferredLanguage? language;
	private PreferredUnitSystem? unitSystem;
	private DefaultServings? defaultServings;
	private DietaryProfile? dietaryProfile;

	public static AppUserProfileBuilder A() => new();

	public AppUserProfileBuilder WithKeycloakUserId(string value) =>
		WithKeycloakUserId(KeycloakUserId.From(value));

	public AppUserProfileBuilder WithKeycloakUserId(KeycloakUserId value)
	{
		keycloakUserId = value;
		return this;
	}

	public AppUserProfileBuilder Named(string value) => Named(DisplayName.From(value));

	public AppUserProfileBuilder Named(DisplayName value)
	{
		displayName = value;
		return this;
	}

	public AppUserProfileBuilder PreferringLanguage(string code)
	{
		language = PreferredLanguage.From(code);
		return this;
	}

	public AppUserProfileBuilder PreferringUnitSystem(string code)
	{
		unitSystem = PreferredUnitSystem.From(code);
		return this;
	}

	public AppUserProfileBuilder WithDefaultServings(int value)
	{
		defaultServings = DefaultServings.From(value);
		return this;
	}

	public AppUserProfileBuilder WithDietaryProfile(DietaryProfile value)
	{
		dietaryProfile = value;
		return this;
	}

	public AppUserProfileAggregate Build()
	{
		var profile = AppUserProfileAggregate.Create(keycloakUserId, displayName);
		if (language is not null)
		{
			profile.SwitchLanguageTo(language);
		}

		if (unitSystem is not null)
		{
			profile.SwitchUnitSystemTo(unitSystem);
		}

		if (defaultServings is not null)
		{
			profile.AdjustDefaultServingsTo(defaultServings);
		}

		if (dietaryProfile is not null)
		{
			profile.UpdateDietaryProfile(dietaryProfile);
		}

		return profile;
	}
}
