using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed class AppUserProfile : AggregateRoot<AppUserProfileIdentifier>
{
    public KeycloakUserId KeycloakUserId { get; private set; } = default!;

    public DisplayName DisplayName { get; private set; } = default!;

    public PreferredLanguage PreferredLanguage { get; private set; } = PreferredLanguage.From("en");

    public PreferredUnitSystem PreferredUnitSystem { get; private set; } = PreferredUnitSystem.From("metric");

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

    public void SwitchLanguageTo(PreferredLanguage language)
    {
        PreferredLanguage = language;
    }

    public void SwitchUnitSystemTo(PreferredUnitSystem unitSystem)
    {
        PreferredUnitSystem = unitSystem;
    }

    public void RenameAs(DisplayName displayName)
    {
        DisplayName = displayName;
    }
}
