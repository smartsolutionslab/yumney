using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record KeycloakUserId
{
    public string Value { get; }

    public KeycloakUserId(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .AndReturn();
    }

    public override string ToString() => Value;
}
