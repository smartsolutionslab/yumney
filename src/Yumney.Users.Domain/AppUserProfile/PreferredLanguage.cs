using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredLanguage
{
    public string Value { get; }

    public PreferredLanguage(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(10)
            .AndReturn();
    }

    public override string ToString() => Value;
}
