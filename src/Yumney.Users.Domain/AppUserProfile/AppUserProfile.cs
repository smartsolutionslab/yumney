using Yumney.Shared.Common;
using Yumney.Shared.Guards;

namespace Yumney.Users.Domain.AppUserProfile;

public sealed class AppUserProfile : AggregateRoot<Guid>
{
    public string KeycloakUserId { get; private set; } = default!;

    public string DisplayName { get; private set; } = default!;

    public string PreferredLanguage { get; private set; } = "en";

    public string PreferredUnitSystem { get; private set; } = "metric";

    private AppUserProfile()
    {
    }

    public static AppUserProfile Create(string keycloakUserId, string displayName)
    {
        Ensure.That(keycloakUserId).IsNotNullOrWhiteSpace();
        Ensure.That(displayName).IsNotNullOrWhiteSpace().HasMaxLength(200);

        return new AppUserProfile
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            DisplayName = displayName,
        };
    }

    public void ChangePreferredLanguage(string language)
    {
        Ensure.That(language).IsNotNullOrWhiteSpace().HasMaxLength(10);
        PreferredLanguage = language;
    }

    public void ChangePreferredUnitSystem(string unitSystem)
    {
        Ensure.That(unitSystem).IsNotNullOrWhiteSpace().HasMaxLength(20);
        PreferredUnitSystem = unitSystem;
    }

    public void RenameAs(string displayName)
    {
        Ensure.That(displayName).IsNotNullOrWhiteSpace().HasMaxLength(200);
        DisplayName = displayName;
    }
}
