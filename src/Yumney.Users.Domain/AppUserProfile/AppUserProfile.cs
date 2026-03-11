using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed class AppUserProfile : AggregateRoot<Guid>
{
    public KeycloakUserId KeycloakUserId { get; private set; } = default!;

    public DisplayName DisplayName { get; private set; } = default!;

    public PreferredLanguage PreferredLanguage { get; private set; } = new("en");

    public PreferredUnitSystem PreferredUnitSystem { get; private set; } = new("metric");

    private AppUserProfile()
    {
    }

    public static AppUserProfile Create(KeycloakUserId keycloakUserId, DisplayName displayName)
    {
        return new AppUserProfile
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            DisplayName = displayName,
        };
    }

    public void ChangePreferredLanguage(PreferredLanguage language)
    {
        PreferredLanguage = language;
    }

    public void ChangePreferredUnitSystem(PreferredUnitSystem unitSystem)
    {
        PreferredUnitSystem = unitSystem;
    }

    public void RenameAs(DisplayName displayName)
    {
        DisplayName = displayName;
    }
}
